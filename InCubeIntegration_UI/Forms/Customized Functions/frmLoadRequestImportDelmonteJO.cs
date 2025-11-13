using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace InCubeIntegration_UI
{
    public partial class frmLoadRequestImportDelmonteJO : Form
    {
        OleDbConnection excelConn;
        string queryString = "", countQuery = "";
        InCubeDatabase db_LoadImport;
        InCubeQuery incubeQry;
        Dictionary<string, WarehouseTransaction> WHT = new Dictionary<string, WarehouseTransaction>();
        enum requiredColumns
        {
            VehicleCode = 0,
            WarehouseCode = 1,
            ItemCode = 2,
            UOM = 3,
            Quantity = 4,
            BatchNo=5
        }

        private class WarehouseTransaction
        {
            public int WarehouseID;
            public string VehicleCode;
            public string WarehouseCode;
            public string Key;
            public Dictionary<string, TransactionDetail> Details;
            public string ProcessResult;
            public int EmployeeID;
            public int OrganizationID;
            public string DocumentSequence;
            public int VehicleID;
            public string TransactionID;
            public string InsertionResult;
            public WarehouseTransaction()
            {
                Details = new Dictionary<string, TransactionDetail>();
                Key = "";
                WarehouseCode = "";
                ProcessResult = "";
                DocumentSequence = "";
                TransactionID = "";
                InsertionResult = "";
            }
        }

        private class TransactionDetail
        {
            public string ItemKey;
            public string ItemCode;
            public string UOM;
            public int Quantity;
            public int ItemID;
            public int PackTypeID;
            public int PackID;
            public string BatchNo; 
            public string ProcessResult;

            public TransactionDetail()
            {
                ItemKey = "";
                ItemCode = "";
                UOM = "";
                ProcessResult = "";
            }
        }

        public frmLoadRequestImportDelmonteJO()
        {
            InitializeComponent();
            db_LoadImport = new InCubeDatabase();
            db_LoadImport.Open("InCube", "frmLoadRequestImportDelmontyJO");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                btnImport.Enabled = false;
                lsvContents.Clear();
                WHT = new Dictionary<string, WarehouseTransaction>();

                if (OpenExcelConnection() == Result.Success)
                {
                    if (CopyExcelContentsToDB() == Result.Success)
                    {
                        if (ProcessData() == Result.Success)
                        {
                            lsvContents.Clear();
                            lsvContents.Columns.Add("ItemCode");
                            lsvContents.Columns.Add("UOM");
                            lsvContents.Columns.Add("Quantity");
                            lsvContents.Columns.Add("BatchNo");
                            lsvContents.Columns.Add("Result");
                            lsvContents.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
                            lsvContents.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.HeaderSize);
                            lsvContents.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.HeaderSize);
                            lsvContents.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.HeaderSize);

                            foreach (KeyValuePair<string, WarehouseTransaction> pair in WHT)
                            {
                                ListViewGroup lsvGrp = new ListViewGroup(pair.Key.PadRight(25) + " [" + pair.Value.ProcessResult + "]");
                                lsvGrp.Tag = pair.Value;

                                lsvContents.Groups.Add(lsvGrp);
                                foreach (KeyValuePair<string, TransactionDetail> detail in pair.Value.Details)
                                {
                                    string[] lsvItemArr = new string[5];
                                    lsvItemArr[0] = detail.Value.ItemCode;
                                    lsvItemArr[1] = detail.Value.UOM;
                                    lsvItemArr[2] = detail.Value.Quantity.ToString();
                                    lsvItemArr[3] = detail.Value.BatchNo;
                                    lsvItemArr[4] = detail.Value.ProcessResult;
                                    ListViewItem lsvItem = new ListViewItem(lsvItemArr, lsvGrp);

                                    switch (detail.Value.ProcessResult)
                                    {
                                        case "Valid":
                                            lsvItem.BackColor = Color.LightBlue;
                                            break;
                                        default:
                                            lsvItem.BackColor = Color.Thistle;
                                            break;
                                    }
                                    lsvContents.Items.Add(lsvItem);
                                }

                            }
                            lsvContents.AutoResizeColumn(4, ColumnHeaderAutoResizeStyle.ColumnContent);
                        }
                    }
                }
                if (lsvContents.Items.Count > 0)
                    btnImport.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (excelConn != null && excelConn.State == ConnectionState.Open)
                    excelConn.Close();
            }
        }

        private Result OpenExcelConnection()
        {
            try
            {
                string excelPath = "";
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select load requests excel sheet";
                ofd.FileName = "";
                ofd.Filter = "Excel files (*.xls,*.xlsx)|*.xls;*.xlsx";
                if (ofd.ShowDialog() == DialogResult.OK && !ofd.FileName.Equals(string.Empty))
                {
                    excelPath = ofd.FileName;
                    txtExcelPath.Text = excelPath;
                }
                else
                    return Result.Failure;

                excelConn = new OleDbConnection();
                try
                {
                    excelConn.ConnectionString = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 12.0;", excelPath);
                    excelConn.Open();
                }
                catch (Exception ex2)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex2.Message, LoggingType.Error, LoggingFiles.UIErrors);
                    try
                    {
                        excelConn.ConnectionString = @"Provider=Microsoft.JET.OLEDB.4.0;Data Source=" + excelPath + ";Extended Properties='Excel 8.0; HDR=Yes;IMEX=1;'";
                        excelConn.Open();
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                        throw new Exception("Failed in openning excel file");
                    }
                }

                DataTable dtSheets = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                string sheet1 = "";
                if (dtSheets.Rows.Count == 0)
                {
                    throw new Exception("File has no sheets !!");
                }
                else if (dtSheets.Rows.Count > 0)
                {
                    frmSelectSheet frm = new frmSelectSheet(dtSheets);
                    frm.ShowDialog();
                    sheet1 = frm.SelectedSheet;
                }
                else
                {
                    sheet1 = dtSheets.Rows[0]["TABLE_NAME"].ToString();
                }

                DataTable dtData = excelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, null);
                string where = " WHERE ";
                queryString = "SELECT ";
                countQuery = "SELECT COUNT(*) FROM [" + sheet1 + "]";
                foreach (string colName in Enum.GetNames(typeof(requiredColumns)))
                {
                    queryString += "[" + colName + "],";
                    where += "[" + colName + "] IS NOT NULL AND ";
                    DataRow[] dr = dtData.Select("TABLE_NAME = '" + sheet1 + "' AND COLUMN_NAME = '" + colName + "'");
                    if (dr.Length == 0)
                    {
                        throw new Exception("Column [" + colName + "] doesn't exist in sheet " + sheet1);
                    }
                }

                queryString = queryString.Substring(0, queryString.Length - 1) + " FROM [" + sheet1 + "] " + where.Substring(0, where.Length - 5);
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                MessageBox.Show(ex.Message);
                return Result.Failure;
            }
        }
        private Result CopyExcelContentsToDB()
        {
            try
            {
                object count = null;
                if (excelConn.State != ConnectionState.Open)
                    excelConn.Open();
                OleDbCommand excelCmd = new OleDbCommand(countQuery, excelConn);
                count = excelCmd.ExecuteScalar();
                if (count != null && count != DBNull.Value && Convert.ToInt16(count) > 0)
                {
                    excelCmd = new OleDbCommand(queryString, excelConn);
                    excelCmd.CommandTimeout = 3600000;
                    OleDbDataReader dr = excelCmd.ExecuteReader();

                    incubeQry = new InCubeQuery(db_LoadImport, "TRUNCATE TABLE LoadRequestTemp");
                    if (incubeQry.ExecuteNonQuery() != InCubeErrors.Success)
                        throw new Exception("Error in reading load request data");

                    try
                    {
                        SqlBulkCopy bulk = new SqlBulkCopy(db_LoadImport.GetConnection());
                        bulk.DestinationTableName = "LoadRequestTemp";
                        bulk.BulkCopyTimeout = 3600000;
                        bulk.WriteToServer(dr);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                        throw new Exception("Error in reading load request data");
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                MessageBox.Show(ex.Message);
                return Result.Failure;
            }
        }
        private Result ProcessData()
        {
            try
            {
                incubeQry = new InCubeQuery(db_LoadImport, "sp_ProcessLoadData");
                if (incubeQry.ExecuteStoredProcedure() == InCubeErrors.Success)
                {
                    incubeQry = new InCubeQuery(db_LoadImport, "SELECT * FROM LoadRequestTemp");
                    if (incubeQry.Execute() == InCubeErrors.Success)
                    {
                        DataTable dtLoadData = incubeQry.GetDataTable();
                        for (int i = 0; i < dtLoadData.Rows.Count; i++)
                        {
                            string Transkey = "Load " + dtLoadData.Rows[i]["VehicleCode"].ToString() + " From " + dtLoadData.Rows[i]["WarehouseCode"].ToString();
                            if (!WHT.ContainsKey(Transkey))
                            {
                                WarehouseTransaction Trans = new WarehouseTransaction();
                                Trans.Key = Transkey;
                                Trans.VehicleCode = dtLoadData.Rows[i]["VehicleCode"].ToString();
                                Trans.WarehouseCode = dtLoadData.Rows[i]["WarehouseCode"].ToString();
                                if (dtLoadData.Rows[i]["Result"].ToString() == "Valid")
                                    Trans.ProcessResult = "Valid";
                                else
                                    Trans.ProcessResult = "Invalid";

                                if (dtLoadData.Rows[i]["WarehouseID"] != null && dtLoadData.Rows[i]["WarehouseID"] != DBNull.Value)
                                    Trans.WarehouseID = Convert.ToInt16(dtLoadData.Rows[i]["WarehouseID"].ToString());
                                if (dtLoadData.Rows[i]["VehicleID"] != null && dtLoadData.Rows[i]["VehicleID"] != DBNull.Value)
                                    Trans.VehicleID = Convert.ToInt16(dtLoadData.Rows[i]["VehicleID"].ToString());
                                if (dtLoadData.Rows[i]["EmployeeID"] != null && dtLoadData.Rows[i]["EmployeeID"] != DBNull.Value)
                                    Trans.EmployeeID = Convert.ToInt16(dtLoadData.Rows[i]["EmployeeID"].ToString());
                                if (dtLoadData.Rows[i]["OrganizationID"] != null && dtLoadData.Rows[i]["OrganizationID"] != DBNull.Value)
                                    Trans.OrganizationID = Convert.ToInt16(dtLoadData.Rows[i]["OrganizationID"].ToString());
                                if (dtLoadData.Rows[i]["DocumentSequence"] != null && dtLoadData.Rows[i]["DocumentSequence"] != DBNull.Value)
                                    Trans.DocumentSequence = dtLoadData.Rows[i]["DocumentSequence"].ToString();
                                WHT.Add(Transkey, Trans);
                            }

                            string ItemKey = dtLoadData.Rows[i]["ItemCode"].ToString() + "-" + dtLoadData.Rows[i]["UOM"].ToString() + "-" + dtLoadData.Rows[i]["BatchNo"].ToString();
                            if (!WHT[Transkey].Details.ContainsKey(ItemKey))
                            {
                                TransactionDetail TD = new TransactionDetail();
                                TD.ItemCode = dtLoadData.Rows[i]["ItemCode"].ToString();
                                TD.UOM = dtLoadData.Rows[i]["UOM"].ToString();
                                TD.Quantity = Convert.ToInt16(dtLoadData.Rows[i]["Quantity"].ToString());
                                TD.ProcessResult = dtLoadData.Rows[i]["Result"].ToString();
                                TD.BatchNo = dtLoadData.Rows[i]["BatchNo"].ToString();
                                if (TD.ProcessResult != "Valid")
                                    WHT[Transkey].ProcessResult = "Invalid";
                                if (dtLoadData.Rows[i]["ItemID"] != null && dtLoadData.Rows[i]["ItemID"] != DBNull.Value)
                                    TD.ItemID = Convert.ToInt16(dtLoadData.Rows[i]["ItemID"].ToString());
                                if (dtLoadData.Rows[i]["PackTypeID"] != null && dtLoadData.Rows[i]["PackTypeID"] != DBNull.Value)
                                    TD.PackTypeID = Convert.ToInt16(dtLoadData.Rows[i]["PackTypeID"].ToString());
                                if (dtLoadData.Rows[i]["PackID"] != null && dtLoadData.Rows[i]["PackID"] != DBNull.Value)
                                    TD.PackID = Convert.ToInt16(dtLoadData.Rows[i]["PackID"].ToString());
                                WHT[Transkey].Details.Add(ItemKey, TD);
                            }
                            else
                            {
                                WHT[Transkey].Details[ItemKey].Quantity += Convert.ToInt16(dtLoadData.Rows[i]["Quantity"].ToString());
                            }
                        }
                    }
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                MessageBox.Show(ex.Message);
                return Result.Failure;
            }
        }

        private void frmLoadRequestImportDelmontyJO_Load(object sender, EventArgs e)
        {
            this.BackColor = CoreGeneral.Common.GeneralConfigurations.IntegrationFormBackColor;
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            InCubeTransaction Trans = new InCubeTransaction();
            InCubeErrors err = InCubeErrors.NotInitialized;
            foreach (KeyValuePair<string, WarehouseTransaction> pair in WHT)
            {
                try
                {
                    err = InCubeErrors.NotInitialized;

                    if (pair.Value.ProcessResult != "Valid")
                        continue;

                    string TransactionID = pair.Value.DocumentSequence;
                    int i = TransactionID.Length - 1;
                    for (i = i; i >= 0; i--)
                        if (!char.IsNumber(TransactionID[i]))
                            break;
                    int numericPartLength = TransactionID.Length - i - 1;
                    long numericpart = long.Parse(TransactionID.Substring(TransactionID.Length - numericPartLength, numericPartLength)) + 1;
                    string alphaPart = "";
                    if (numericPartLength != TransactionID.Length)
                        alphaPart = TransactionID.Substring(0, TransactionID.Length - numericPartLength);
                    if (numericpart % 2 != 1)
                        numericpart++;
                    pair.Value.TransactionID = alphaPart + numericpart.ToString().PadLeft(numericPartLength, '0');

                    err = Trans.BeginTransaction(db_LoadImport);
                    if (err != InCubeErrors.Success)
                        continue;

                    //                    string headerInsert = string.Format(@"INSERT INTO WarehouseTransaction (WarehouseID,TransactionID,TransactionTypeID,TransactionDate,RequestedBy,ImplementedBy,Synchronized
                    //,ProductionDate,RefWarehouseID,Posted,WarehouseTransactionStatusID,CreationSourceID,TransactionOperationID,RouteHistoryID,LoadDate
                    //,CreationReason,DivisionID,OrganizationID,OutVoucherReasonID,ForLiquidation)
                    //VALUES ({0},'{1}',1,GETDATE(),{2},{3},0
                    //,GETDATE(),{4},0,1,1,1,-1,GETDATE()
                    //,1,3,{5},-1,0)", pair.Value.VehicleID, pair.Value.TransactionID, pair.Value.EmployeeID
                    //                    , CoreGeneral.Common.CurrentSession.EmployeeID, pair.Value.WarehouseID, pair.Value.OrganizationID);
                    //                    incubeQry = new InCubeQuery(headerInsert, db_LoadImport);
                    //                    err = incubeQry.ExecuteNoneQuery(Trans);
                    //                    if (err != InCubeErrors.Success)
                    //                        continue;
                    QueryBuilder QueryBuilderObject = null;
                    foreach (KeyValuePair<string, TransactionDetail> detail in pair.Value.Details)
                    {
                        //                        string detailInsert = string.Format(@"INSERT INTO WhTransDetail (WarehouseID,TransactionID,ZoneID,PackID,ExpiryDate,Quantity,Balanced,BatchNo,PackStatusID,DivisionID
                        //,Downloaded,RequestedQuantity)
                        //VALUES ({0},'{1}',1,{2},'1990-01-01',{3},0,'{4}',0,3,0,{3})"
                        //                            , pair.Value.VehicleID, pair.Value.TransactionID, detail.Value.PackID, detail.Value.Quantity,detail.Value.BatchNo.Trim()=="" ? "1990/01/01": detail.Value.BatchNo.Trim());
                        //                        incubeQry = new InCubeQuery(detailInsert, db_LoadImport);
                        //                        err = incubeQry.ExecuteNoneQuery(Trans);
                        ////// update stock //////////
                        string batch = detail.Value.BatchNo.Trim() == "" ? "1990/01/01" : detail.Value.BatchNo.Trim();
                        QueryBuilderObject = new QueryBuilder();
                        err = ExistObject("WarehouseStock", "PackID", "WarehouseID = " + pair.Value.VehicleID + " AND ZoneID = 1 AND PackID = " + detail.Value.PackID + " AND ExpiryDate = '1990-01-01' AND BatchNo = '" + batch + "'", db_LoadImport,Trans);
                        if (err != InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("WarehouseID", pair.Value.VehicleID.ToString());
                            QueryBuilderObject.SetField("ZoneID", "1");
                            QueryBuilderObject.SetField("PackID", detail.Value.PackID.ToString());
                            QueryBuilderObject.SetField("ExpiryDate", "'19900101'");
                            QueryBuilderObject.SetField("BatchNo", "'" + batch + "'");
                            QueryBuilderObject.SetField("SampleQuantity", "0");
                            QueryBuilderObject.SetField("Quantity", detail.Value.Quantity.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", detail.Value.Quantity.ToString());
                       

                          err=  QueryBuilderObject.InsertQueryString("WarehouseStock", db_LoadImport,Trans);
                        }
                        else 
                        {
                            QueryBuilderObject.SetField("Quantity", "Quantity+"+ detail.Value.Quantity.ToString());
                            QueryBuilderObject.SetField("BaseQuantity", detail.Value.Quantity.ToString());
                        err= QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + pair.Value.VehicleID + " AND ZoneID = 1 AND PackID = " + detail.Value.PackID + " AND ExpiryDate = '19900101' AND BatchNo = '" + batch + "'", db_LoadImport,Trans);
                        }

                                           ///////////////////////////

                        if (err != InCubeErrors.Success)
                            continue;
                    }

                    //if (err == InCubeErrors.Success)
                    //{
                    //    string docSeqUpdate = string.Format(@"UPDATE DocumentSequence SET MaxWarehouseTransactionID = '{0}' WHERE EmployeeID = {1}"
                    //        , pair.Value.TransactionID, pair.Value.EmployeeID);
                    //    incubeQry = new InCubeQuery(docSeqUpdate, db_LoadImport);
                    //    err = incubeQry.ExecuteNoneQuery(Trans);
                    //}
                }
                catch (Exception ex)
                {
                    err = InCubeErrors.Error;
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.UIErrors);
                }
                finally
                {
                    if (err == InCubeErrors.Success)
                    {
                        Trans.Commit();
                        pair.Value.InsertionResult = "Success";
                    }
                    else
                    {
                        Trans.Rollback();
                    }
                }
            }
            btnImport.Enabled = false;
            foreach (ListViewGroup GRP in lsvContents.Groups)
            {
                WarehouseTransaction T = (WarehouseTransaction)GRP.Tag;
                if (T.InsertionResult == "Success")
                    GRP.Header += ", Added successfully [" + T.TransactionID + "]";
            }
        }
        protected InCubeErrors ExistObject(string TableName, string Field, string WhereCondition, InCubeDatabase _db, InCubeTransaction Tran)
        {
            if (!_db.IsOpened())
            {
                throw new Exception("The Connection State is Closed");
            }

            InCubeErrors err;
            string QueryStr = "Select " + Field + " From " + TableName + " Where " + WhereCondition;
            InCubeQuery Query = new InCubeQuery(_db, QueryStr);
            Query.Execute(Tran);
            err = Query.FindFirst();
            Query.Close();
            return err;
        }

    }
}