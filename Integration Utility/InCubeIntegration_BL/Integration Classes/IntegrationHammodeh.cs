using System; 
using System.Text;
using System.Data;
using InCubeLibrary;  
using System.IO; 
using System.Globalization;  
using System.Security.Cryptography;
 
using System.ServiceModel;
using InCubeIntegration_DAL;
using InCubeIntegration_BL.Hammodeh_WS;
using System.Xml;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace InCubeIntegration_BL
{
    class IntegrationHammodeh : IntegrationBase
    {
        QueryBuilder QueryBuilderObject = new QueryBuilder();
        private string _encryptionKey = "!@#$%^&**&^%$#@!";
        InCubeErrors err;
        private long UserID;
        string DateFormat = "dd/MMM/yyyy";
        string DateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";
        DataTable dtCustomerTables = null;
        CultureInfo EsES = new CultureInfo("es-ES");
        InCubeTransaction dbTrans = null;
        InCubeQuery incubeQuery;
        int TotalInserted;
        int TotalUpdated;
        int TotalIgnored;
        bool exist = false;
        InCubeDatabase db_res;
        BackgroundWorker bgwCheckProgress;
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        string StagingTable = "";
        string _WarehouseID = "-1";
       
        Hammodeh_WS.ServiceClient ProxyService;
        string errorMessage;
        //Initialize Company 
        Hammodeh_WS.MCompany mCompany;

        public IntegrationHammodeh(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            if (CoreGeneral.Common.CurrentSession.loginType != LoginType.WindowsService)
            {
                db_res = new InCubeDatabase();
                db_res.Open("InCube", "IntegrationHammodeh");
            }
            //NetTcpBinding binding = new NetTcpBinding();
            //binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
            ////binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
            ///
            var binding = new BasicHttpBinding()
            {
                Name = "BindingName",
                MaxBufferSize = 2147483647,
                MaxReceivedMessageSize = 2147483647,
                CloseTimeout = new TimeSpan(0, 10, 0) 
                ,SendTimeout= new TimeSpan(0, 10, 0),
                ReceiveTimeout= new TimeSpan(0, 10, 0)
            };

            ProxyService = new ServiceClient(binding, new EndpointAddress(CoreGeneral.Common.GeneralConfigurations.WS_URL));
            //ProxyService.Endpoint.Address = new EndpointAddress("");
           
             

            string _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_dataSourceFilePath);
            //Conn = new OdbcConnection(strConnectionString);
            mCompany = new MCompany();
            mCompany.Username = xmlDoc.SelectSingleNode("Connections/Username").InnerText;
            mCompany.Password = xmlDoc.SelectSingleNode("Connections/Password").InnerText;
            mCompany.DatabaseServer = xmlDoc.SelectSingleNode("Connections/DatabaseServer").InnerText;
            mCompany.DatabaseName = xmlDoc.SelectSingleNode("Connections/DatabaseName").InnerText;
            mCompany.DbUsername = xmlDoc.SelectSingleNode("Connections/DbUsername").InnerText;
            mCompany.DbPassword = xmlDoc.SelectSingleNode("Connections/DbPassword").InnerText;
            mCompany.LiscenceServer = xmlDoc.SelectSingleNode("Connections/LiscenceServer").InnerText;
            mCompany.ServerType = BoDataServerTypes.dst_HANADB;
            
            UserID = CurrentUserID;
            bgwCheckProgress = new BackgroundWorker();
            bgwCheckProgress.DoWork += new DoWorkEventHandler(bgw_CheckProgress);
            bgwCheckProgress.WorkerSupportsCancellation = true;
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

        #region UpdateItem

        public override void UpdateItem()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DefaultOrganization();

            DataTable DT_Item = ProxyService.HH_Get_Item(mCompany, out errorMessage);

            QueryBuilderObject.SetField("InActive", "1");
            err = QueryBuilderObject.UpdateQueryString("Item", "ItemType <> 7 and ItemType <> 8", db_vms);

            ClearProgress();
            SetProgressMax(DT_Item.Rows.Count);

            foreach (DataRow row in DT_Item.Rows)
            {
                ReportProgress("Updating Items");


                //Item Code 0	
                //Item description English 1	
                //Item description Arabic	2
                //Item division 3
                //Division description	4
                //Item category	5
                //Category description	6
                //Brand	7
                //Origin	8
                //Pack 	9
                //Pack quantity	10
                //Pack barcode    11
                char[] arr =  { '0' };
                string ItemCode = row["ItemCode"].ToString().TrimStart( arr);
                string itemDescriptionEnglish = row["itemdescription"].ToString().Trim();
                string itemDescriptionArabic = itemDescriptionEnglish;// row["ItemDescF"].ToString().Trim();
              
                string CategoryCode = row["itemcategorycode"].ToString().Trim();
                string CategoryNameEnglish = row["categorydescription"].ToString().Trim();
                string DivisionCode = CategoryCode;
                string DivisionNameEnglish = CategoryNameEnglish;
                string Orgin = row["UOMStock"].ToString().Trim();
                string PackDescriptionEnglish = "Õ»…";// row["ItemUoM"].ToString().Trim();
                //if (PackDescriptionEnglish != "Outer") continue;
                string packQty = row["ItemQty"].ToString().Trim();
                string barcode = row["ItemBarcode"].ToString().Trim();
                string itemTax = row["itemTax"].ToString().Trim();
                
                if (PackDescriptionEnglish.Trim() == "") PackDescriptionEnglish = "CTN";
                if (packQty.Trim() == "") packQty = "1";

                if (ItemCode == string.Empty)
                    continue;

               

                #region ItemDivision

                string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                if (DivisionID == string.Empty)
                {
                    DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("CreatedDate",   DateTime.Now );
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate",  DateTime.Now );
                    QueryBuilderObject.InsertQueryString("Division", db_vms);

                    QueryBuilderObject.SetField("DivisionID", DivisionID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);

                    QueryBuilderObject.SetField("DivisionID", DivisionID);  // Arabic Description
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + DivisionNameEnglish + "'");
                    QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                }

                #endregion

                #region ItemCategory

                string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "'", db_vms);

                if (ItemCategoryID == string.Empty)
                {
                    ItemCategoryID = GetFieldValue("ItemCategory", "isnull(MAX(ItemCategoryID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                    QueryBuilderObject.SetField("ItemCategoryCode", "'" + CategoryCode + "'");
                    QueryBuilderObject.SetField("DivisionID", DivisionID.ToString());
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                    QueryBuilderObject.InsertQueryString("ItemCategory", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + CategoryNameEnglish + "'");
                    QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);  // Arabic Description
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + CategoryNameEnglish + "'");
                    QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                }
                else
                {
                    string _existItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "' AND DivisionID =" + DivisionID.ToString(), db_vms);
                    if (ItemCategoryID == string.Empty)
                    {
                        WriteMessage("\r\n");
                        WriteMessage(" Item Category " + CategoryNameEnglish + " is defined twice , the duplicated division : " + DivisionNameEnglish);
                    }
                }

                #endregion

                #region PackType

                string PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + PackDescriptionEnglish + "' AND LanguageID = 1", db_vms);

                if (PacktypeID == string.Empty)
                {
                    PacktypeID = GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                    QueryBuilderObject.InsertQueryString("PackType", db_vms);

                    QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + PackDescriptionEnglish + "'");
                    QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                    QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "N'" + PackDescriptionEnglish + "'");
                    QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                }

                #endregion

                string ItemID = "";

                ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "'", db_vms);
                if (ItemID == string.Empty)
                {
                    ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                }
                 
                #region Item

                string ExistItem = GetFieldValue("Item", "ItemID", "ItemID = " + ItemID, db_vms);
                if (ExistItem != string.Empty) // Exist Item --- Update Query
                {
                    TOTALUPDATED++;

                    QueryBuilderObject.SetStringField("PackDefinition", itemTax);
                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetField("Origin", "'" + Orgin + "'");
                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.UpdateQueryString("Item", " ItemID = " + ItemID, db_vms);

                }
                else // New Item --- Insert Query
                {
                    TOTALINSERTED++;

                    QueryBuilderObject.SetField("ItemID", ItemID);
                    QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());
                    QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                    QueryBuilderObject.SetField("InActive", "0");
                    QueryBuilderObject.SetStringField("PackDefinition", itemTax);

                    QueryBuilderObject.SetField("Origin", "'" + Orgin + "'"); 

                    QueryBuilderObject.SetField("ItemType", "1");
                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.InsertQueryString("Item", db_vms);
                }

                #endregion

                #region ItemLanguage

                ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 1", db_vms);
                if (ExistItem != string.Empty)
                {
                    QueryBuilderObject.SetField("Description", "'" + itemDescriptionEnglish + "'");
                    QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);

                }
                else
                {
                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + itemDescriptionEnglish + "'");
                    QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                }

                if (itemDescriptionArabic != string.Empty)
                {
                    ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 2", db_vms); // ARABIC
                    if (ExistItem != string.Empty)
                    {
                        QueryBuilderObject.SetField("Description", "N'" + itemDescriptionArabic + "'");
                        QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 2", db_vms);
                    }
                    else
                    {
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + itemDescriptionArabic + "'");
                        QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                    }
                }
                #endregion

                #region UPDATE/INSERT PACK

                int PackID = 1;

                ExistItem = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms);
                if (ExistItem != string.Empty)
                {
                    PackID = int.Parse(GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " and PackTypeID = " + PacktypeID, db_vms));

                    QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                    QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                }
                else
                {
                    PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("PackID", PackID.ToString());
                    QueryBuilderObject.SetField("Barcode", "'" + barcode + "'");
                    QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                    QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                    QueryBuilderObject.SetField("Quantity", packQty);
                    QueryBuilderObject.SetField("EquivalencyFactor", "0");
                    QueryBuilderObject.InsertQueryString("Pack", db_vms);

                }

                #endregion
            }

            DT_Item.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion
         
        #region UpdateCustomer





        #endregion

        #region UpdateVehicles
        public override void UpdateMainWarehouse()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            object field = new object();

            DefaultOrganization();

            MWareHouse mWareHouse = new MWareHouse();
            DataTable DT_Warehouse = ProxyService.HH_Get_Stores(mCompany, mWareHouse, out errorMessage);

            ClearProgress();
            SetProgressMax(DT_Warehouse.Rows.Count);

            foreach (DataRow row in DT_Warehouse.Rows)
            {
                 ReportProgress( "Updating Vehicles" );
               
                //Warehouse Code	
                //Description	
                //Plate number	
                //Type 
                //Salesperson code
                //OrganizationID

                string WarehouseCode = row["WarehouseCode"].ToString().Trim();
                string WarehouceName = row["Description"].ToString().Trim();
                string VehicleRegNum = row["WarehouseCode"].ToString().Trim();
                string WarehouseType = "1";
                if (row["WarehouseCode"].ToString().ToUpper().StartsWith("CV"))
                {
                    WarehouseType = "2";
                }
                string SalesmanCode = row["salespersoncode"].ToString().Trim();
                string OrgID = "";

                if (OrgID == string.Empty) OrgID = OrganizationID.ToString();
                if (WarehouseCode == string.Empty)
                    continue;

                string Address = "";
                string WarehouseID = "";

                WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode ='" + WarehouseCode + "'", db_vms);
                if (WarehouseID == string.Empty)
                {
                    WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                }

                AddUpdateWarehouse(WarehouseID, WarehouseCode, WarehouceName, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, WarehouseType, OrgID);

            }

            DT_Warehouse.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< WAREHOUSE >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string WarehouseType, string OrgID)
        {
            InCubeErrors err;

            string ExitWarehouse = "";

            ExitWarehouse = GetFieldValue("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (ExitWarehouse != string.Empty) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;

                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.UpdateQueryString("Warehouse", " WarehouseID = " + WarehouseID, db_vms);
            }
            else  // New Warehouse --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("WarehouseTypeID", WarehouseType);

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Warehouse", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms);
            if (ExitWarehouse != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExitWarehouse != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                QueryBuilderObject.UpdateQueryString("WarehouseLanguage", " WarehouseID =" + WarehouseID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + "'");
                QueryBuilderObject.SetField("Address", "N'" + Address + "'");
                QueryBuilderObject.InsertQueryString("WarehouseLanguage", db_vms);
            }

            #region WarehouseZone/Vehicle/VehicleSalesPerson

            ExitWarehouse = GetFieldValue("WarehouseZone", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.InsertQueryString("WarehouseZone", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 1", db_vms);
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + WarehouceName + " Zone'");
                QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }

            ExitWarehouse = GetFieldValue("WarehouseZoneLanguage", "WarehouseID", "WarehouseID = " + WarehouseID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExitWarehouse == string.Empty)
            {
                QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                QueryBuilderObject.SetField("ZoneID", "1");
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + WarehouceName + " Zone'");
                QueryBuilderObject.InsertQueryString("WarehouseZoneLanguage", db_vms);
            }

            if (WarehouseType == "2")
            {
                ExitWarehouse = GetFieldValue("Vehicle", "VehicleID", "VehicleID = " + WarehouseID, db_vms);
                if (ExitWarehouse == string.Empty)
                {
                    QueryBuilderObject.SetField("VehicleID", WarehouseID);
                    QueryBuilderObject.SetField("PlateNO", "'" + VehicleRegNum + "'");
                    QueryBuilderObject.SetField("TypeID", "1");

                    QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                    QueryBuilderObject.InsertQueryString("Vehicle", db_vms);
                }
            }

            string SalesPersonID = GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + SalesmanCode + "'", db_vms);

            if (SalesPersonID == string.Empty)
            {
                WriteMessage("\r\n");
                WriteMessage("Warning Vehicle Code : (" + WarehouseCode + ") is not assigned to any salesperson");
                WriteMessage("\r\n");
            }

            string VehicleID = GetFieldValue("Vehicle", "VehicleID", "VehicleID=" + WarehouseID, db_vms);

            if (!SalesPersonID.Trim().Equals(string.Empty) && !VehicleID.Trim().Equals(string.Empty))
            {
                ExitWarehouse = GetFieldValue("EmployeeVehicle", "VehicleID", "EmployeeID = " + SalesPersonID, db_vms);
                if (ExitWarehouse == string.Empty)
                {
                    QueryBuilderObject.SetField("VehicleID", VehicleID);
                    QueryBuilderObject.SetField("EmployeeID", SalesPersonID);
                    QueryBuilderObject.InsertQueryString("EmployeeVehicle", db_vms);
                }
                else
                {
                    WriteMessage("\r\n");
                    WriteMessage("Warning Salesperson Code : (" + SalesmanCode + ") is assigned to 2 vehicles , Second Vehicle Code : (" + WarehouseCode + ") this row is skipped");
                    WriteMessage("\r\n");
                }
            }

            #endregion
        }

        #endregion

        #region UpdateSalesPerson

        public override void UpdateSalesPerson()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            DefaultOrganization();
            //UpdateBanks();

            object field = new object();

            DataTable DT_User = ProxyService.HH_Get_User(mCompany, out errorMessage);

            ClearProgress();
            SetProgressMax(DT_User.Rows.Count);

            foreach (DataRow row in DT_User.Rows)
            {
                ReportProgress("Updating Salespersons" );
     

                //Employee Code	
                //Employee name English	
                //Employee name Arabic	
                //Phone	
                //Credit limit	
                //Balance	
                //Division

                //string NationalIDNumber = row["employeecode"].ToString().Trim();
                string EmployeeCode = row["employeecode"].ToString().Trim();
                string EmployeeNameEnglish = row["employeename"].ToString().Trim();
                string EmployeeNameArabic = row["employeename"].ToString().Trim();
                string CashAccount = row["CashAccount"].ToString().Trim();
                string CheckAccount = row["CheckAccount"].ToString().Trim();
                string Phone = "";
                string CreditLimit = "99999999";
                string Balance = "0";
                string DivisionCode = "01";
                string orgID = "";

                if (EmployeeCode == string.Empty)
                    continue;

                if (orgID.ToString() == "") orgID = OrganizationID.ToString();
                string SalespersonID = "";
                if (CashAccount.ToString() == "") CashAccount = "0";
                if (CheckAccount.ToString() == "") CheckAccount = "0";
                SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode = '" + EmployeeCode + "'", db_vms);
                if (SalespersonID == string.Empty)
                {
                    SalespersonID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);
                }

                string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                //if (DivisionID == string.Empty)
                //{
                //    DivisionID = "1";
                //}

                AddUpdateSalesperson(SalespersonID, EmployeeCode, "", EmployeeNameArabic, EmployeeNameEnglish, Phone, ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, CreditLimit, Balance, orgID, CashAccount, CheckAccount);
                 
            }

            DT_User.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        private void AddUpdateSalesperson(string SalespersonID, string SalespersonCode, string NationalIDNumber, string SalespersonNameArabic, string SalespersonNameEnglish, string Phone, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string CreditLimit, string Balance, string orgID, string CashAccount, string CheckAccount)
        {
            string ExistEmployee = "";
            bool updated = false;
            ExistEmployee = GetFieldValue("Employee", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)// New Salesperon --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                //QueryBuilderObject.SetField("NationalIDNumber", NationalIDNumber);
                QueryBuilderObject.SetField("OrganizationID", orgID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", "2");
                QueryBuilderObject.SetStringField("HourlyRegularRate", CashAccount);
                QueryBuilderObject.SetStringField("HourlyOvertimeRate", CheckAccount);

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Employee", db_vms);
            }
            else
            {
                updated = true;
                QueryBuilderObject.SetField("OrganizationID", orgID);
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetStringField("HourlyRegularRate", CashAccount);
                QueryBuilderObject.SetStringField("HourlyOvertimeRate", CheckAccount);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID = " + SalespersonID, db_vms);
            }

            ExistEmployee = GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", db_vms);
            if (ExistEmployee != string.Empty)
            {
                updated = true;
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish + "'");
                QueryBuilderObject.SetField("Address", "''");
                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            }

            ExistEmployee = GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", db_vms);
            if (ExistEmployee != string.Empty)
            {
                updated = true;
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                QueryBuilderObject.UpdateQueryString("EmployeeLanguage", "EmployeeID = " + SalespersonID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic + "'");
                QueryBuilderObject.SetField("Address", "''");
                QueryBuilderObject.InsertQueryString("EmployeeLanguage", db_vms);
            }

            err = ExistObject("Operator", "OperatorID", "OperatorID = " + SalespersonID, db_vms);
            if (err != InCubeErrors.Success)
            {
                QueryBuilderObject.SetField("OperatorID", SalespersonID);
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("FrontOffice", "1");
                QueryBuilderObject.SetField("LoginTypeID", "1");
                QueryBuilderObject.SetField("OperatorPassword", "'" + EncryptData(SalespersonCode) + "'");
                err = QueryBuilderObject.InsertQueryString("Operator", db_vms);
            }
            else
            {
                updated = true;
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("OperatorPassword", "'" + EncryptData(SalespersonCode) + "'");
                err = QueryBuilderObject.UpdateQueryString("Operator", "OperatorID = " + SalespersonID, db_vms);
            }

            err = ExistObject("EmployeeOperator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (err == InCubeErrors.DBNoMoreRows)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OperatorID", SalespersonID);
                QueryBuilderObject.InsertQueryString("EmployeeOperator", db_vms);
            }

            InCubeQuery DivisionQuery = new InCubeQuery(db_vms, "SELECT  DivisionID FROM Division");
            DivisionQuery.Execute();
            DataTable DT = new DataTable();
            DT = DivisionQuery.GetDataTable();
            DivisionQuery.Close();

            foreach (DataRow row in DT.Rows)
            {
                string _divisionID = row[0].ToString();

                ExistEmployee = GetFieldValue("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + _divisionID, db_vms);
                if (ExistEmployee == string.Empty)
                {
                    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                    QueryBuilderObject.SetField("DivisionID", _divisionID);
                    QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
                }
            }

            ExistEmployee = GetFieldValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", orgID);
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            int AccountID = 1;

            ExistEmployee = GetFieldValue("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", orgID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.InsertQueryString("AccountEmp", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            if (updated)
                TOTALUPDATED++;
        }
        private string EncryptData(string data)
        {
            RijndaelManaged RijndaelCipher = new RijndaelManaged();
            PasswordDeriveBytes secretKey;
            ICryptoTransform encryptor;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            string encryptedData;
            try
            {
                byte[] plainText = System.Text.Encoding.Unicode.GetBytes(data);
                byte[] salt = System.Text.Encoding.ASCII.GetBytes(_encryptionKey.Length.ToString());
                secretKey = new PasswordDeriveBytes(_encryptionKey, salt);
                //Creates a symmetric encryptor object. 
                encryptor = RijndaelCipher.CreateEncryptor(secretKey.GetBytes(32), secretKey.GetBytes(16));
                memoryStream = new System.IO.MemoryStream();
                //Defines a stream that links data streams to cryptographic transformations
                cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainText, 0, plainText.Length);
                //Writes the final state and clears the buffer
                cryptoStream.FlushFinalBlock();
                byte[] CipherBytes = memoryStream.ToArray();
                encryptedData = Convert.ToBase64String(CipherBytes);
            }
            catch (Exception ex)
            {
                encryptedData = data;
            }
            finally
            {
                if (memoryStream != null) memoryStream.Close();
                if (cryptoStream != null) cryptoStream.Close();
            }
            return encryptedData;
        }
        #endregion
        #region SendInvoices
        public override void SendInvoices()
        {
            try
            {
                #region(Member Data)
                object TranID = "";
                object TranDate = "";
                object Customer = "";
                object Employee = "";
                object Discount = "";
                object CreationReason = "";
                object UOM = "";
                object ItemCode = "";
                object salesTransTypeID = "";
                object netAmount = "";
                object Quantity = "";
                object Price = "";
                object TaxPercentage = "";
                object tax = "";

               
                object WarehouseCode = "";
                int DocNum;
                int transId = 0;
                DataSet ds;
                DataTable dtInvoiceHeader;
                DataTable dtInvoiceDetails;
                bool HDResult = false, HDResultSp, TransIDSp, DocNumSp;
                string QueryString;
                int   processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                WriteMessage("\r\n" + "Sending Invoices");
                #endregion
              

                #region(Build Data Set)
                ds = new DataSet();

                dtInvoiceHeader = new DataTable("tblHeader");
                dtInvoiceHeader.Columns.Add("TransactionID");
                dtInvoiceHeader.Columns.Add("EmployeeCode");
                dtInvoiceHeader.Columns.Add("CustomerCode");
                dtInvoiceHeader.Columns.Add("TransactionDate");
                dtInvoiceHeader.Columns.Add("VanCode");

                dtInvoiceDetails = new DataTable("tblDetails");
                dtInvoiceDetails.Columns.Add("ItemCode");
                dtInvoiceDetails.Columns.Add("UnitPrice");
                dtInvoiceDetails.Columns.Add("Qty");
                dtInvoiceDetails.Columns.Add("TaxCode");
                dtInvoiceDetails.Columns.Add("DicountPerc");
                dtInvoiceDetails.Columns.Add("TotalNetAmt");
                dtInvoiceDetails.Columns.Add("UoMCode");

                ds.Tables.Add(dtInvoiceHeader);
                ds.Tables.Add(dtInvoiceDetails);
                ds.Tables.Add(new DataTable("tblCheque"));
                #endregion

                #region(Fill Data Set)
                QueryString = string.Format(@"SELECT     
                                         TR.TransactionID, 
                                         E.EmployeeCode ,
                                         CO.CustomerCode,
                                         TR.TransactionDate, 
                                         TR.CreationReason,
                                        W.WarehouseCode

                                         FROM [Transaction] TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID
                                         INNER JOIN EmployeeVehicle EV ON TR.EmployeeID = EV.EmployeeID
                                         INNER JOIN Warehouse W ON EV.VehicleID = W.WarehouseID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID
                                         INNER JOIN Customer C ON CO.CustomerID = C.CustomerID

                                         WHERE TR.Synchronized = 0 AND (TR.Voided = 0 OR TR.Voided IS NULL) AND TR.TransactionTypeID in( 1,3) AND C.New = 0
                                         AND TR.TransactionDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) AND TR.TransactionDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)"
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day);

                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    try
                    {

                        res = Result.UnKnown;
                        ds.Tables["tblHeader"].Clear();
                        ds.Tables["tblDetails"].Clear();

                        #region(Fill Header)
                        err = GetSalesTransactionInformation.GetField("TransactionID", ref TranID);
                        err = GetSalesTransactionInformation.GetField("EmployeeCode", ref Employee);
                        err = GetSalesTransactionInformation.GetField("TransactionDate", ref TranDate);
                        err = GetSalesTransactionInformation.GetField("CustomerCode", ref Customer);
                        err = GetSalesTransactionInformation.GetField("WarehouseCode", ref WarehouseCode);
                        err = GetSalesTransactionInformation.GetField("CreationReason", ref CreationReason);
                        ReportProgress("Sending Transaction: " + TranID);
                        WriteMessage("\r\n" + TranID + ": ");

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(23, TranID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Sales_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TranID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate transaction [" + TranID + "]");
                            throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                        }

                        DateTime _tranDate = DateTime.Parse(TranDate.ToString());

                        DataRow newInvoiceHeaderRow = ds.Tables["tblHeader"].NewRow();

                        newInvoiceHeaderRow["TransactionID"] = TranID.ToString();
                        newInvoiceHeaderRow["EmployeeCode"] = Employee.ToString();
                        newInvoiceHeaderRow["CustomerCode"] = Customer.ToString();
                        newInvoiceHeaderRow["TransactionDate"] = _tranDate.ToString("yyyy/MM/dd");
                        newInvoiceHeaderRow["VanCode"] = WarehouseCode.ToString();

                        ds.Tables["tblHeader"].Rows.Add(newInvoiceHeaderRow);
                        #endregion

                        #region (Fill Details)
                        switch (CreationReason.ToString())
                        {
                            case "1":
                                QueryString = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  I.Origin UOM,
                                                  TD.Price,
                                                  TD.Discount, 
                                                  TD.Tax ,
                                                  TD.SalesTransactionTypeID

                                                  FROM TransactionDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
                                                    
                                                  WHERE PTL.LanguageID = 1 AND TD.TransactionID = '{0}'", TranID);
                                break;

                            case "4":
                            case "9":
                            case "10":
                            case "11":
                            case "17":
                            case "18":
                            case "19":
                            case "20":
                            case "21":
                            case "22":
                            case "23":

                                QueryString = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  PTL.Description UOM,
                                                  TD.Price,
                                                  TD.Discount,
                                                  0 Tax,
                                                  TD.SalesTransactionTypeID

                                                  FROM TransactionDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
                                                    INNER JOIN PriceDefinition PD ON TD.PackID = PD.PacKID AND PriceListID = (SELECT KeyValue FROM Configuration WHERE KeyName = 'ConsumerPriceListID')
                                                  
												  WHERE PTL.LanguageID = 1 AND TD.TransactionID = '{0}'
												  ", TranID);
                                break;
                        }

                        InCubeQuery dtlQry = new InCubeQuery(db_vms, QueryString);
                        err = dtlQry.Execute();
                        err = dtlQry.FindFirst();

                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }

                        while (err == InCubeErrors.Success)
                        {
                            err = dtlQry.GetField("SalesTransactionTypeID", ref salesTransTypeID);
                            err = dtlQry.GetField("ItemCode", ref ItemCode);
                            err = dtlQry.GetField("Quantity", ref Quantity);
                            err = dtlQry.GetField("UOM", ref UOM);
                            err = dtlQry.GetField("Price", ref Price);
                            err = dtlQry.GetField("Discount", ref Discount);
                            err = dtlQry.GetField("tax", ref tax);

                            DataRow newInvoiceDetailsRow = ds.Tables["tblDetails"].NewRow();

                            newInvoiceDetailsRow["ItemCode"] = ItemCode.ToString();
                            newInvoiceDetailsRow["Qty"] = Quantity.ToString();
                            newInvoiceDetailsRow["TaxCode"] = "S16";
                            newInvoiceDetailsRow["UoMCode"] = UOM.ToString();

                            decimal price = decimal.Parse(Price.ToString());
                            decimal qty = decimal.Parse(Quantity.ToString());
                            decimal discount = decimal.Parse(Discount.ToString());
                            decimal net = price * qty;
                            newInvoiceDetailsRow["UnitPrice"] = price.ToString();
                            if (net == 0)
                            {
                                newInvoiceDetailsRow["DicountPerc"] = (discount / net * 100).ToString();
                            }
                            else
                            {
                                newInvoiceDetailsRow["DicountPerc"] = 0;
                            }

                         
                            newInvoiceDetailsRow["TotalNetAmt"] = net.ToString();

                            if (CreationReason.ToString() != "1")
                            {
                                newInvoiceDetailsRow["UnitPrice"] = "0";
                                newInvoiceDetailsRow["TotalNetAmt"] = "0";
                            }

                            ds.Tables["tblDetails"].Rows.Add(newInvoiceDetailsRow);

                            err = dtlQry.FindNext();
                        }
                        #endregion

                        #region(Send Invoice)
                        try
                        {
                            
                            ProxyService.HH_Add_InvoiceNoPayment(ds, mCompany, out errorMessage, out transId, out DocNum);
                            WriteXML(ds, HDResult, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

                            if (errorMessage == "" )
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - OK");

                                res = Result.Success;
                                QueryBuilderObject.SetField("Synchronized", "1");
                                QueryBuilderObject.SetField("Notes", "'" + transId.ToString() + "'");
                                err = QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage(", Failure in updating the sync flag, check InCubeLog.txt");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), "Invoice sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
                                }
                            }
                            else
                            {
                                result.Append("\r\nError in SAP("+ errorMessage + ")");
                                res = Result.WebServiceConnectionError;
                                WriteMessage("\r\n" + TranID.ToString() + " - Rejected by SAP, check errorInv.log");
                            }
                        }
                        catch (Exception sendEx)
                        {
                            WriteXML(ds, false, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

                            result.Append(sendEx.Message);
                            if (res == Result.UnKnown)
                            {
                                res = Result.Failure;
                                WriteMessage("Unhandled exception !!");
                            }
                            WriteMessage("\r\n" + TranID.ToString() + " - Failure in sending to SAP, check errorInv.log");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        result.Append( ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                        WriteMessage("\r\n" + TranID.ToString() + " - Failure in preparing data, check InCubeLog.txt");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendReturn()
        {
            try
            {
                #region(Member Data)
                object TranID = "";
                object TranDate = "";
                object Customer = "";
                object Employee = "";
                object Discount = "";
                object CreationReason = "";
                object UOM = "";
                object ItemCode = "";
                object salesTransTypeID = "";
                object netAmount = "";
                object Quantity = "";
                object Price = "";
                object TaxPercentage = "";
                object tax = "";

                object WarehouseCode = "";
                int DocNum;
                int transId = 0;
                DataSet ds;
                DataTable dtInvoiceHeader;
                DataTable dtInvoiceDetails;
                bool HDResult = false, HDResultSp, TransIDSp, DocNumSp;
                string QueryString;
                int processID = 0;
                StringBuilder result = new StringBuilder();
                Result res = Result.UnKnown;
                #endregion

                WriteMessage("\r\n" + "Sending Invoices");

                #region(Build Data Set)
                ds = new DataSet();

                dtInvoiceHeader = new DataTable("tblHeader");
                dtInvoiceHeader.Columns.Add("CreditNumber");
                dtInvoiceHeader.Columns.Add("CreditUserID");
                dtInvoiceHeader.Columns.Add("CreditClientID");
                dtInvoiceHeader.Columns.Add("CreditDate", typeof(DateTime));
                dtInvoiceHeader.Columns.Add("DiscountTypeValue");
                dtInvoiceHeader.Columns.Add("Remarks");

                dtInvoiceDetails = new DataTable("tblDetails");
                dtInvoiceDetails.Columns.Add("ItemCode");
                dtInvoiceDetails.Columns.Add("UnitPrice");
                dtInvoiceDetails.Columns.Add("Qty");
                dtInvoiceDetails.Columns.Add("TaxCode");
                dtInvoiceDetails.Columns.Add("DicountPerc");
                dtInvoiceDetails.Columns.Add("TotalNetAmt");
                dtInvoiceDetails.Columns.Add("UoMCode");
                dtInvoiceDetails.Columns.Add("RetReason");

                ds.Tables.Add(dtInvoiceHeader);
                ds.Tables.Add(dtInvoiceDetails);
                ds.Tables.Add(new DataTable("tblCheque"));
                #endregion

                #region(Fill Data Set)
                QueryString = string.Format(@"SELECT     
                                         TR.TransactionID, 
                                         E.EmployeeCode ,
                                         CO.CustomerCode,
                                         TR.TransactionDate, 
                                         TR.CreationReason,
                                        W.WarehouseCode

                                         FROM [Transaction] TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID
                                         INNER JOIN EmployeeVehicle EV ON TR.EmployeeID = EV.EmployeeID
                                         INNER JOIN Warehouse W ON EV.VehicleID = W.WarehouseID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID
                                         INNER JOIN Customer C ON CO.CustomerID = C.CustomerID

                                         WHERE TR.Synchronized = 0 AND (TR.Voided = 0 OR TR.Voided IS NULL) AND TR.TransactionTypeID in( 2,4) AND C.New = 0
                                         AND TR.TransactionDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) AND TR.TransactionDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)"
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day);

                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    try
                    {
                        res = Result.UnKnown;

                        ds.Tables["tblHeader"].Clear();
                        ds.Tables["tblDetails"].Clear();

                        #region(Fill Header)
                        err = GetSalesTransactionInformation.GetField("TransactionID", ref TranID);
                        err = GetSalesTransactionInformation.GetField("EmployeeCode", ref Employee);
                        err = GetSalesTransactionInformation.GetField("TransactionDate", ref TranDate);
                        err = GetSalesTransactionInformation.GetField("CustomerCode", ref Customer);
                        err = GetSalesTransactionInformation.GetField("WarehouseCode", ref WarehouseCode);
                        err = GetSalesTransactionInformation.GetField("CreationReason", ref CreationReason);


                        ReportProgress("Sending Returns: " + TranID);
                        WriteMessage("\r\n" + TranID + ": ");

                        Dictionary<int, string> filters = new Dictionary<int, string>();
                        filters.Add(25, TranID.ToString());
                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Returns_S, new List<string>(filters.Values), processID, 60);
                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
                        {
                            res = Result.Duplicate;
                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [transaction] SET Synchronized = 1 WHERE transactionid = '" + TranID + "'");
                            incubeQuery.ExecuteNonQuery();
                            result.Append("Duplicate transaction [" + TranID + "]");
                            throw (new Exception("Transaction already sent  check table  Int_ExecutionDetails!!"));
                        }

                        DateTime _tranDate = DateTime.Parse(TranDate.ToString());

                        DataRow newInvoiceHeaderRow = ds.Tables["tblHeader"].NewRow();

                        newInvoiceHeaderRow["CreditNumber"] = TranID.ToString();
                        newInvoiceHeaderRow["CreditUserID"] = Employee.ToString();
                        newInvoiceHeaderRow["CreditClientID"] = Customer.ToString();
                        newInvoiceHeaderRow["CreditDate"] = _tranDate;
                        newInvoiceHeaderRow["DiscountTypeValue"] = "0";
                        newInvoiceHeaderRow["Remarks"] = "";

                        ds.Tables["tblHeader"].Rows.Add(newInvoiceHeaderRow);
                        #endregion

                        #region (Fill Details)
                  
                                QueryString = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  I.Origin UOM,
                                                  TD.Price,
                                                  TD.Discount, 
                                                  TD.Tax ,
                                                  TD.SalesTransactionTypeID

                                                  FROM TransactionDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
                                                   
                                                  WHERE PTL.LanguageID = 1 AND TD.TransactionID = '{0}'", TranID);
                             

                        InCubeQuery dtlQry = new InCubeQuery(db_vms, QueryString);
                        err = dtlQry.Execute();
                        err = dtlQry.FindFirst();

                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }

                        while (err == InCubeErrors.Success)
                        {
                            err = dtlQry.GetField("SalesTransactionTypeID", ref salesTransTypeID);
                            err = dtlQry.GetField("ItemCode", ref ItemCode);
                            err = dtlQry.GetField("Quantity", ref Quantity);
                            err = dtlQry.GetField("UOM", ref UOM);
                            err = dtlQry.GetField("Price", ref Price);
                            err = dtlQry.GetField("Discount", ref Discount);
                            err = dtlQry.GetField("tax", ref tax);

                            DataRow newInvoiceDetailsRow = ds.Tables["tblDetails"].NewRow();

                            newInvoiceDetailsRow["ItemCode"] = ItemCode.ToString();
                            newInvoiceDetailsRow["Qty"] = Quantity.ToString();
                            newInvoiceDetailsRow["TaxCode"] = "S16";
                            newInvoiceDetailsRow["UoMCode"] = UOM.ToString();

                            decimal price = decimal.Parse(Price.ToString());
                            decimal qty = decimal.Parse(Quantity.ToString());
                            decimal discount = decimal.Parse(Discount.ToString());
                            decimal net = price * qty;
                            newInvoiceDetailsRow["UnitPrice"] = price.ToString();

                            newInvoiceDetailsRow["DicountPerc"] = (discount / net * 100).ToString();

                            newInvoiceDetailsRow["TotalNetAmt"] = net.ToString();

                            //if (CreationReason.ToString() != "1")
                            //{
                            //    newInvoiceDetailsRow["UnitPrice"] = "0";
                            //    newInvoiceDetailsRow["TotalNetAmt"] = "0";
                            //}

                            ds.Tables["tblDetails"].Rows.Add(newInvoiceDetailsRow);

                            err = dtlQry.FindNext();
                        }
                        #endregion

                        #region(Send Invoice)
                        try
                        {
                            ProxyService.HH_Add_CreditNote(ds, mCompany, out errorMessage, out transId, out DocNum);
                            WriteXML(ds, HDResult, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

                            if (errorMessage == "")
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - OK");

                                QueryBuilderObject.SetField("Synchronized", "1");
                                QueryBuilderObject.SetField("Notes", "'" + transId.ToString() + "'");
                                err = QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage(", Failure in updating the return sync flag , check InCubeLog.txt");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), "Invoice sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
                                }
                            }
                            else
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - Rejected by SAP, check errorInv.log");
                            }
                        }
                        catch (Exception sendEx)
                        {
                            result.Append(sendEx.Message);
                            if (res == Result.UnKnown)
                            {
                                res = Result.Failure;
                                WriteMessage("Unhandled exception !!");
                            }
                            WriteMessage("\r\n" + TranID.ToString() + " - Failure in sending to SAP, check errorInv.log");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        result.Append(ex.Message);
                        if (res == Result.UnKnown)
                        {
                            res = Result.Failure;
                            WriteMessage("Unhandled exception !!");
                        }
                        WriteMessage("\r\n" + TranID.ToString() + " - Failure in preparing data, check InCubeLog.txt");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    finally
                    {
                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        public override void SendOrders()
        {
            try
            {
                #region(Member Data)
                object TranID = "";
                object TranDate = "";
                object Customer = "";
                object Employee = "";
                object Discount = "";
                object DesiredDeliveryDate = "";
                object Note = "";
                object UOM = "";
                object ItemCode = "";
                object salesTransTypeID = "";
                object netAmount = "";
                object Quantity = "";
                object Price = "";
                object TaxPercentage = "";
                int DocNum;
                int transId = 0;
                DataSet ds;
                DataTable dtInvoiceHeader;
                DataTable dtInvoiceDetails;
                bool HDResult = false;
                string QueryString;
                #endregion

                WriteMessage("\r\n" + "Sending Invoices");

                #region(Build Data Set)
                ds = new DataSet();

                dtInvoiceHeader = new DataTable("tblHeader");
                dtInvoiceHeader.Columns.Add("SalesOrderClientID");
                dtInvoiceHeader.Columns.Add("SalesOrderNumber");
                dtInvoiceHeader.Columns.Add("SalesOrderUserID");
                dtInvoiceHeader.Columns.Add("SalesOrderDate");
                dtInvoiceHeader.Columns.Add("SalesOrderDeliveryDate");
                dtInvoiceHeader.Columns.Add("DicountTypeValue");
                dtInvoiceHeader.Columns.Add("SalesOrderRemark");

                dtInvoiceDetails = new DataTable("tblDetails");
                dtInvoiceDetails.Columns.Add("ItemCode");
                dtInvoiceDetails.Columns.Add("UnitPrice");
                dtInvoiceDetails.Columns.Add("Qty");
                dtInvoiceDetails.Columns.Add("TaxCode");
                dtInvoiceDetails.Columns.Add("DicountPerc");
                dtInvoiceDetails.Columns.Add("TotalNetAmt");
                dtInvoiceDetails.Columns.Add("UoMCode");

                ds.Tables.Add(dtInvoiceHeader);
                ds.Tables.Add(dtInvoiceDetails);
                ds.Tables.Add(new DataTable("tblCheque"));
                #endregion

                #region(Fill Data Set)
                QueryString = string.Format(@"Select      TR.OrderID, 
                                         E.EmployeeCode EmployeeCode,
                                         CO.CustomerCode,
                                         TR.OrderDate,
										 TR.DesiredDeliveryDate 
                                         ,(select top(1) note from SalesOrderNote n where n.OrderID=tr.orderid and n.Note<>'') Note
                                         FROM Salesorder TR
                                         INNER JOIN Employee E ON TR.EmployeeID = E.EmployeeID
                                         INNER JOIN CustomerOutlet CO ON TR.CustomerID = CO.CustomerID AND TR.OutletID = CO.OutletID
                                         INNER JOIN Customer C ON CO.CustomerID = C.CustomerID

                                         WHERE TR.Synchronized = 0  AND C.New = 0
                                         AND TR.OrderDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) AND TR.OrderDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)"
                                                 , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day);

                if (Filters.EmployeeID != -1)
                {
                    QueryString += " AND TR.EmployeeID = " + Filters.EmployeeID;
                }

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    try
                    {

                        ds.Tables["tblHeader"].Clear();
                        ds.Tables["tblDetails"].Clear();

                        #region(Fill Header)
                        err = GetSalesTransactionInformation.GetField("OrderID", ref TranID);
                        err = GetSalesTransactionInformation.GetField("EmployeeCode", ref Employee);
                        err = GetSalesTransactionInformation.GetField("OrderDate", ref TranDate);
                        err = GetSalesTransactionInformation.GetField("CustomerCode", ref Customer);
                        err = GetSalesTransactionInformation.GetField("DesiredDeliveryDate", ref DesiredDeliveryDate);
                        err = GetSalesTransactionInformation.GetField("Note", ref Note);

                        DateTime _tranDate = DateTime.Parse(TranDate.ToString());

                        DataRow newInvoiceHeaderRow = ds.Tables["tblHeader"].NewRow();

                        newInvoiceHeaderRow["SalesOrderNumber"] = TranID.ToString();
                        newInvoiceHeaderRow["SalesOrderUserID"] = Employee.ToString();
                        newInvoiceHeaderRow["SalesOrderClientID"] = Customer.ToString();
                        newInvoiceHeaderRow["SalesOrderDate"] = _tranDate.ToString("yyyy/MM/dd");
                        newInvoiceHeaderRow["SalesOrderDeliveryDate"] = DateTime.Parse(DesiredDeliveryDate.ToString()).ToString("yyyy/MM/dd");
                        newInvoiceHeaderRow["SalesOrderRemark"] = Note;
                        newInvoiceHeaderRow["DicountTypeValue"] = "0";

                        ds.Tables["tblHeader"].Rows.Add(newInvoiceHeaderRow);
                        #endregion

                        #region (Fill Details)

                        QueryString = string.Format(@"SELECT     
                                                  I.ItemCode,
                                                  TD.Quantity,
                                                  I.Origin UOM,
                                                  TD.Price,
                                                  TD.Discount,
                                                  TD.Tax ,
                                                  TD.SalesTransactionTypeID

                                                  FROM SalesOrderDetail TD
                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
                                               
                                                  
                                                  WHERE PTL.LanguageID = 1 AND TD.orderid = '{0}'", TranID);


                        InCubeQuery dtlQry = new InCubeQuery(db_vms, QueryString);
                        err = dtlQry.Execute();
                        err = dtlQry.FindFirst();

                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }

                        while (err == InCubeErrors.Success)
                        {
                            err = dtlQry.GetField("SalesTransactionTypeID", ref salesTransTypeID);
                            err = dtlQry.GetField("ItemCode", ref ItemCode);
                            err = dtlQry.GetField("Quantity", ref Quantity);
                            err = dtlQry.GetField("UOM", ref UOM);
                            err = dtlQry.GetField("Price", ref Price);
                            err = dtlQry.GetField("Discount", ref Discount);
                            err = dtlQry.GetField("tax", ref TaxPercentage);

                            DataRow newInvoiceDetailsRow = ds.Tables["tblDetails"].NewRow();

                            newInvoiceDetailsRow["ItemCode"] = ItemCode.ToString();
                            newInvoiceDetailsRow["Qty"] = Quantity.ToString();
                            newInvoiceDetailsRow["TaxCode"] = "S16";
                            newInvoiceDetailsRow["UoMCode"] = UOM.ToString();
                            newInvoiceDetailsRow["DicountPerc"] = Discount.ToString();

                            decimal price = decimal.Parse(Price.ToString());
                            decimal qty = decimal.Parse(Quantity.ToString());
                            decimal discount = decimal.Parse(Discount.ToString());
                            decimal tax = decimal.Parse(TaxPercentage.ToString());
                            decimal net = price * qty * (1 - (discount / 100)) * (1 + (tax / 100));
                            newInvoiceDetailsRow["UnitPrice"] = price.ToString();

                            newInvoiceDetailsRow["TotalNetAmt"] = net;

                            ds.Tables["tblDetails"].Rows.Add(newInvoiceDetailsRow);

                            err = dtlQry.FindNext();
                        }
                        #endregion

                        #region(Send Orders)    
                        try
                        {
                            try
                            {
                                ProxyService.HH_Add_SalesOrder(ds, mCompany, out errorMessage, out transId, out DocNum);

                            }
                            catch (Exception ex)
                            {
                                HDResult = false;
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                            }
                            WriteXML(ds, HDResult, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

                            if (errorMessage == "")
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - OK");

                                QueryBuilderObject.SetField("Synchronized", "1");
                                QueryBuilderObject.SetField("Description", "'" + transId.ToString() + "'");
                                err = QueryBuilderObject.UpdateQueryString("[Salesorder]", " OrderID = '" + TranID.ToString() + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage(", Failure in updating the orders sync flag, check InCubeLog.txt");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), "Order sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
                                }
                            }
                            else
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - Rejected by SAP, check errorInv.log");
                            }
                        }
                        catch (Exception sendEx)
                        {
                            WriteMessage("\r\n" + TranID.ToString() + " - Failure in sending orders to SAP, check errorInv.log");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        WriteMessage("\r\n" + TranID.ToString() + " - Failure in preparing orders data, check InCubeLog.txt");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public override void SendTransfers()
        {
            try
            {
                #region(Member Data)
                object TranID = "";
                object TranDate = "";
                object WarehouseCode = "";
                object Employee = "";
                object Discount = "";
                object RefWarehouseCode = "";
                object Note = "";
                object UOM = "";
                object ItemCode = "";
                object salesTransTypeID = "";
                object netAmount = "";
                object Quantity = "";
                object Price = "";
                object TaxPercentage = "";
                int DocNum;
                int transId = 0;
                DataSet ds;
                DataTable dtInvoiceHeader;
                DataTable dtInvoiceDetails;
                bool HDResult = false;
                string QueryString;
                #endregion

                WriteMessage("\r\n" + "Sending Invoices");

                #region(Build Data Set)
                ds = new DataSet();

                dtInvoiceHeader = new DataTable("tblHeader");
                dtInvoiceHeader.Columns.Add("FromWarehouse");
                dtInvoiceHeader.Columns.Add("ToWarehouse");
                dtInvoiceHeader.Columns.Add("SalesPersonCode");

                dtInvoiceDetails = new DataTable("tblDetails");
                dtInvoiceDetails.Columns.Add("ItemCode");
                dtInvoiceDetails.Columns.Add("FromWarehouse");
                dtInvoiceDetails.Columns.Add("ToWarehouse");
                dtInvoiceDetails.Columns.Add("Quantity"); 

                ds.Tables.Add(dtInvoiceHeader);
                ds.Tables.Add(dtInvoiceDetails);
                #endregion

                #region(Fill Data Set)
                string salespersonFilter = "";
                if (Filters.EmployeeID != -1)
                {
                    salespersonFilter += " AND wt.RequestedBy = " + Filters.EmployeeID;
                }
                QueryString = string.Format(@"select wt.transactionid,wt.TransactionDate,e.EmployeeCode,w.WarehouseCode,rw.WarehouseCode RefWarehouseCode ,wt.Notes,e.Phone TempletID
 from WarehouseTransaction wt inner join Organization o on wt.Organizationid=o.Organizationid
LEFT JOIN Warehouse w on wt.WarehouseID=W.WarehouseID 
LEFT JOIN Warehouse rw on wt.RefWarehouseID=RW.WarehouseID 
INNER JOIN Employee e on wt.RequestedBy=e.employeeid 
where wt.Synchronized<>1 and (
 (TransactionOperationID=1 and WarehouseTransactionStatusID in(1,2))) 
					  AND wt.transactiondate >= '{0}' AND wt.transactiondate < '{1}' 
                  {2}"
                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
              

                InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

                err = GetSalesTransactionInformation.Execute();
                err = GetSalesTransactionInformation.FindFirst();

                while (err == InCubeErrors.Success)
                {
                    try
                    {

                        ds.Tables["tblHeader"].Clear();
                        ds.Tables["tblDetails"].Clear();

                        #region(Fill Header)
                        err = GetSalesTransactionInformation.GetField("transactionid", ref TranID);
                        err = GetSalesTransactionInformation.GetField("EmployeeCode", ref Employee); 
                        err = GetSalesTransactionInformation.GetField("WarehouseCode", ref WarehouseCode);
                        err = GetSalesTransactionInformation.GetField("RefWarehouseCode", ref RefWarehouseCode); 
                        
                        DataRow newInvoiceHeaderRow = ds.Tables["tblHeader"].NewRow();

                       // newInvoiceHeaderRow["SalesOrderNumber"] = TranID.ToString();
                        newInvoiceHeaderRow["SalesPersonCode"] = Employee.ToString();
                        newInvoiceHeaderRow["FromWarehouse"] = RefWarehouseCode.ToString();
                        newInvoiceHeaderRow["ToWarehouse"] =  WarehouseCode.ToString(); 

                        ds.Tables["tblHeader"].Rows.Add(newInvoiceHeaderRow);
                        #endregion

                        #region (Fill Details)

                        QueryString = string.Format(@"					SELECT      
sum(WhTransDetail.Quantity) Quantity, 
item. ItemCode       
FROM WhTransDetail  INNER JOIN
Pack ON WhTransDetail.PackID = Pack.PackID INNER JOIN
Item ON Pack.ItemID = Item.ItemID INNER JOIN 
PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID

WHERE (PackTypeLanguage.LanguageID = 1) 
 AND (WhTransDetail.TransactionID like '{0}')
group by 
WhTransDetail.TransactionID,
item.itemcode 
 ", TranID);


                        InCubeQuery dtlQry = new InCubeQuery(db_vms, QueryString);
                        err = dtlQry.Execute();
                        err = dtlQry.FindFirst();

                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }

                        while (err == InCubeErrors.Success)
                        {
                             err = dtlQry.GetField("ItemCode", ref ItemCode);
                            err = dtlQry.GetField("Quantity", ref Quantity);
                            
                            DataRow newInvoiceDetailsRow = ds.Tables["tblDetails"].NewRow();

                            newInvoiceDetailsRow["ItemCode"] = ItemCode.ToString();
                            newInvoiceDetailsRow["Quantity"] = Quantity.ToString();
                            newInvoiceDetailsRow["FromWarehouse"] = RefWarehouseCode.ToString();
                            newInvoiceDetailsRow["ToWarehouse"] = WarehouseCode.ToString();

                            

                            ds.Tables["tblDetails"].Rows.Add(newInvoiceDetailsRow);

                            err = dtlQry.FindNext();
                        }
                        #endregion

                        #region(Send transfer)    
                        try
                        {
                            try
                            {
                                ProxyService.HH_Add_StockTransferRequest(ds, mCompany, out errorMessage, out transId, out DocNum);

                            }
                            catch (Exception ex)
                            {
                                HDResult = false;
                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                            }
                            WriteXML(ds, HDResult, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

                            if (errorMessage == "")
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - OK");

                                QueryBuilderObject.SetField("Synchronized", "1");
                                QueryBuilderObject.SetField("Note", "isnull(Note,'')+'(" + transId.ToString() + ")'");
                                err = QueryBuilderObject.UpdateQueryString("[WarehouseTransaction]", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage(", Failure in updating the transfer sync flag, check InCubeLog.txt");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), "Transfer sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
                                }
                            }
                            else
                            {
                                WriteMessage("\r\n" + TranID.ToString() + " - Rejected by SAP, check errorInv.log");
                            }
                        }
                        catch (Exception sendEx)
                        {
                            WriteMessage("\r\n" + TranID.ToString() + " - Failure in sending transfer to SAP, check errorInv.log");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        WriteMessage("\r\n" + TranID.ToString() + " - Failure in preparing transfer data, check InCubeLog.txt");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    err = GetSalesTransactionInformation.FindNext();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion
        //        public override void SendOrders()
        //        {
        //            try
        //            {

        //                 string salespersonFilter = "", SalesDocType = "", netTotal = "", SalesType = "", SalesMode = "";
        //                string CustomerCode = "", OutletCode = "", Hdiscount = "", Notes = "", TransactionID = "", WarehouseCode = "";
        //                DateTime TransactionDate;
        //                int OrgID = 0, processID = 0;
        //                StringBuilder result = new StringBuilder();
        //                Result res = Result.UnKnown;
        //                string QtyField = "";
        //                #region(Build Data Set)
        //                DataSet ds = new DataSet();
        //                DataTable dtInvoiceHeader;
        //                DataTable dtInvoiceDetails;

        //                dtInvoiceHeader = new DataTable("tblHeader");
        //                dtInvoiceHeader.Columns.Add("InvoiceNumber");
        //                dtInvoiceHeader.Columns.Add("InvoiceUserID");
        //                dtInvoiceHeader.Columns.Add("InvoiceClientID");
        //                dtInvoiceHeader.Columns.Add("InvoiceDate", typeof(DateTime));
        //                dtInvoiceHeader.Columns.Add("DicountTypeValue");

        //                dtInvoiceDetails = new DataTable("tblDetails");
        //                dtInvoiceDetails.Columns.Add("ItemCode");
        //                dtInvoiceDetails.Columns.Add("UnitPrice");
        //                dtInvoiceDetails.Columns.Add("Qty");
        //                dtInvoiceDetails.Columns.Add("TaxCode");
        //                dtInvoiceDetails.Columns.Add("DicountPerc");
        //                dtInvoiceDetails.Columns.Add("TotalNetAmt");
        //                dtInvoiceDetails.Columns.Add("UnitCode");
        //                dtInvoiceDetails.Columns.Add("Freight1Code");
        //                dtInvoiceDetails.Columns.Add("Freight1Amt");
        //                dtInvoiceDetails.Columns.Add("Freight2Code");
        //                dtInvoiceDetails.Columns.Add("Freight2Amt");
        //                dtInvoiceDetails.Columns.Add("Freight3Code");
        //                dtInvoiceDetails.Columns.Add("Freight3Amt");

        //                ds.Tables.Add(dtInvoiceHeader);
        //                ds.Tables.Add(dtInvoiceDetails);
        //                ds.Tables.Add(new DataTable("tblCheque"));
        //                #endregion

        //                if (Filters.EmployeeID != -1)
        //                {
        //                    salespersonFilter = "AND T.EmployeeID = " + Filters.EmployeeID;
        //                }
        //                string invoicesHeader = string.Format(@"SELECT   T.orderid transactionid,   T.orderdate transactiondate ,  V.Barcode,e.Employeecode , C.CustomerCode, CO.CustomerCode OutletCode , t.Discount,convert(varchar,t.DesiredDeliveryDate)+' - '+isnull((select top(1) Note from SalesOrderNote where OrderID=t.orderid),'') Notes ,2 SalesMode            FROM salesorder T 
        //                    INNER JOIN CustomerOutlet CO ON CO.CustomerID = T.CustomerID AND CO.OutletID = T.OutletID
        //                    INNER JOIN Customer  C  ON C .CustomerID = T.CustomerID 
        //                    INNER JOIN Organization O ON O.OrganizationID = T.OrganizationID
        //                    INNER JOIN Employee E ON E.EmployeeID = T.EmployeeID
        //					LEFT JOIN EmployeeVehicle ev on e.EmployeeID=ev.EmployeeID
        //				    LEFT JOIN Warehouse V ON V.WarehouseID = ev.VehicleID 
        //                    WHERE T.Synchronized = 0  and t.OrderTypeID=1 
        //					  AND T.orderdate >= '{0}' AND T.orderdate < '{1}' 
        //                  {2}
        //                   and orderstatusid in (1,2) /*and T.TransactionID = 'INV-VC11-0071-000040'*/"
        //                    , Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.AddDays(1).ToString("yyyy-MM-dd"), salespersonFilter);
        //                incubeQuery = new InCubeQuery(db_vms, invoicesHeader);


        //                if (incubeQuery.Execute() != InCubeErrors.Success)
        //                {
        //                    res = Result.Failure;
        //                    throw (new Exception("Order header query failed !!"));
        //                }



        //                DataTable dtInvoices = incubeQuery.GetDataTable();
        //                if (dtInvoices.Rows.Count == 0)
        //                    WriteMessage("There is no Order to send ..");
        //                else
        //                    SetProgressMax(dtInvoices.Rows.Count);

        //                for (int i = 0; i < dtInvoices.Rows.Count; i++)
        //                {
        //                    try
        //                    {
        //                        res = Result.UnKnown;
        //                        processID = 0;
        //                        result = new StringBuilder();
        //                        TransactionID = dtInvoices.Rows[i]["Transactionid"].ToString();
        //                        ReportProgress("Sending Transaction: " + TransactionID);
        //                        WriteMessage("\r\n" + TransactionID + ": ");
        //                        Dictionary<int, string> filters = new Dictionary<int, string>();
        //                        filters.Add(11, TransactionID);
        //                        processID = execManager.LogIntegrationBegining(TriggerID, -1, filters);

        //                        Result lastRes = GetLastExecutionResultForEntry(IntegrationField.Orders_S, new List<string>(filters.Values), processID, 60);
        //                        if (lastRes == Result.Success || lastRes == Result.Duplicate)
        //                        {
        //                            res = Result.Duplicate;
        //                            incubeQuery = new InCubeQuery(db_vms, "UPDATE [salesorder] SET Synchronized = 1 WHERE orderid = '" + TransactionID + "'");
        //                            incubeQuery.ExecuteNonQuery();
        //                            result.Append("Duplicate order [" + TransactionID + "]");
        //                            throw (new Exception("order already sent  check table  Int_ExecutionDetails!!"));
        //                        }


        //                        TransactionDate = Convert.ToDateTime(dtInvoices.Rows[i]["transactiondate"]);
        //                        CustomerCode = dtInvoices.Rows[i]["CustomerCode"].ToString();
        //                        WarehouseCode = dtInvoices.Rows[i]["Barcode"].ToString();
        //                        string Salesperson = dtInvoices.Rows[i]["Employeecode"].ToString();
        //                        SalesMode = dtInvoices.Rows[i]["SalesMode"].ToString();
        //                        SalesType = "83";// dtInvoices.Rows[i]["SalesType"].ToString();
        //                        Notes = dtInvoices.Rows[i]["Notes"].ToString();
        //                        Hdiscount = "0";// decimal.Parse(dtInvoices.Rows[i]["discount"].ToString()).ToString("F4");

        //                        OutletCode = dtInvoices.Rows[i]["OutletCode"].ToString();


        //                        if (SalesMode != "1")
        //                            SalesMode = "0";

        //                        if (WarehouseCode == "") WarehouseCode = "1";


        //                        string invoiceDetails = string.Format(@"SELECT      
        //sum(SalesOrderDetail.Quantity) Quantity,
        //SalesOrderDetail.Price, 
        //SalesOrderDetail.basePrice, 
        //sum(SalesOrderDetail.Price*SalesOrderDetail.Quantity* SalesOrderDetail.Discount/100)  Discount,
        // convert(int,pack.Width) ItemCode,  
        //convert(int,pack.Height) UOMID,     
        //--sum((SalesOrderDetail.Price*SalesOrderDetail.Quantity)-(SalesOrderDetail.Price*SalesOrderDetail.Quantity* SalesOrderDetail.Discount/100))*  SalesOrderDetail.Tax /100 tax
        //SalesOrderDetail.Tax    
        //,SalesOrderDetail.salestransactiontypeid ItemType
        //FROM SalesOrderDetail  INNER JOIN
        //Pack ON SalesOrderDetail.PackID = Pack.PackID INNER JOIN
        //Item ON Pack.ItemID = Item.ItemID INNER JOIN 
        //PackTypeLanguage ON Pack.PackTypeID = PackTypeLanguage.PackTypeID
        //WHERE (PackTypeLanguage.LanguageID = 1) 
        // AND (SalesOrderDetail.OrderID like '{0}')
        //group by 
        //SalesOrderDetail.orderid,
        //SalesOrderDetail.Price,  
        //SalesOrderDetail.Tax , 
        //pack.Width, 
        //pack.Height, 
        //SalesOrderDetail.basePrice, 
        // SalesOrderDetail.salestransactiontypeid   
        //,SalesOrderDetail.CustomerID,SalesOrderDetail.OutletID
        //ORDER BY pack.Width
        //", TransactionID);
        //                        incubeQuery = new InCubeQuery(db_vms, invoiceDetails);
        //                        if (incubeQuery.Execute() != InCubeErrors.Success)
        //                        {
        //                            res = Result.Failure;
        //                            throw (new Exception("order details query failed !!"));
        //                        }

        //                        DataTable dtDetails = incubeQuery.GetDataTable();
        //                        string allDetails = "";
        //                        for (int j = 0; j < dtDetails.Rows.Count; j++)
        //                        {
        //                            string ItemCode = "", UOM = "", Quantity = "", Price = "", BasePrice = "", Tax = "", discount = "", Type = "", bonus = "0";

        //                            ItemCode = dtDetails.Rows[j]["ItemCode"].ToString();
        //                            UOM = dtDetails.Rows[j]["UOMID"].ToString();
        //                            Quantity = decimal.Parse(dtDetails.Rows[j]["Quantity"].ToString()).ToString();//.ToString("#0.000");
        //                            Price = decimal.Parse(dtDetails.Rows[j]["Price"].ToString()).ToString();//Convert.ToDecimal(dtDetails.Rows[j]["Price"]).ToString("#0.0000");
        //                            Tax = decimal.Parse(dtDetails.Rows[j]["Tax"].ToString()).ToString();
        //                            discount = decimal.Parse(dtDetails.Rows[j]["discount"].ToString()).ToString();
        //                            Type = dtDetails.Rows[j]["ItemType"].ToString();
        //                            Price = decimal.Parse(Price.ToString()).ToString();
        //                            BasePrice = dtDetails.Rows[j]["basePrice"].ToString();
        //                            if (Type == "4")
        //                            {
        //                                // QtyField = "bonus";
        //                                QtyField = "quantity";
        //                                Price = BasePrice;
        //                                discount = (decimal.Parse(BasePrice) * decimal.Parse(Quantity)).ToString();
        //                            }
        //                            else
        //                                QtyField = "quantity";

        //                          //  allDetails += string.Format(detailsTemp, UOM, Price, ItemCode, discount, Quantity, "{", "}", QtyField, Tax) + ",";


        //                        }
        //                        while (err == InCubeErrors.Success)
        //                        {
        //                            try
        //                            {

        //                                ds.Tables["tblHeader"].Clear();
        //                                ds.Tables["tblDetails"].Clear();

        //                                #region(Fill Header)
        //                                err = GetSalesTransactionInformation.GetField("TransactionID", ref TranID);
        //                                err = GetSalesTransactionInformation.GetField("EmployeeCode", ref Employee);
        //                                err = GetSalesTransactionInformation.GetField("TransactionDate", ref TranDate);
        //                                err = GetSalesTransactionInformation.GetField("CustomerCode", ref Customer);
        //                                err = GetSalesTransactionInformation.GetField("DiscountPercent", ref Discount);
        //                                err = GetSalesTransactionInformation.GetField("CreationReason", ref CreationReason);

        //                                DateTime _tranDate = DateTime.Parse(TranDate.ToString());

        //                                DataRow newInvoiceHeaderRow = ds.Tables["tblHeader"].NewRow();

        //                                newInvoiceHeaderRow["InvoiceNumber"] = TranID.ToString();
        //                                newInvoiceHeaderRow["InvoiceUserID"] = Employee.ToString();
        //                                newInvoiceHeaderRow["InvoiceClientID"] = Customer.ToString();
        //                                newInvoiceHeaderRow["InvoiceDate"] = _tranDate;
        //                                newInvoiceHeaderRow["DicountTypeValue"] = "0";

        //                                ds.Tables["tblHeader"].Rows.Add(newInvoiceHeaderRow);
        //                                #endregion

        //                                #region (Fill Details)
        //                                switch (CreationReason.ToString())
        //                                {
        //                                    case "1":
        //                                        QueryString = string.Format(@"SELECT     
        //                                                  I.ItemCode,
        //                                                  TD.Quantity,
        //                                                  PTL.Description UOM,
        //                                                  TD.Price,
        //                                                  TD.Discount,
        //                                                  ISNULL(FT.PL102Tax,0) Freight1Amt,
        //                                                  ISNULL(FT.PL42Tax,0) Freight2Amt,
        //                                                  TD.Tax Freight3Amt,
        //                                                  TD.SalesTransactionTypeID

        //                                                  FROM TransactionDetail TD
        //                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
        //                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
        //                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
        //                                                  LEFT OUTER JOIN FixedTaxes FT ON P.PackID = FT.PackID

        //                                                  WHERE PTL.LanguageID = 1 AND TD.TransactionID = '{0}'", TranID);
        //                                        break;

        //                                    case "4":
        //                                    case "9":
        //                                    case "10":
        //                                    case "11":
        //                                    case "17":
        //                                    case "18":
        //                                    case "19":
        //                                    case "20":
        //                                    case "21":
        //                                    case "22":
        //                                    case "23":

        //                                        QueryString = string.Format(@"SELECT     
        //                                                  I.ItemCode,
        //                                                  TD.Quantity,
        //                                                  PTL.Description UOM,
        //                                                  TD.Price,
        //                                                  TD.Discount,
        //                                                  ISNULL(FT.PL102Tax,0) Freight1Amt,
        //                                                  ISNULL(FT.PL42Tax,0) Freight2Amt,
        //                                                  PD.Price/(1+PD.Tax/100) * (PD.Tax/100) * TD.Quantity Freight3Amt,
        //                                                  TD.SalesTransactionTypeID

        //                                                  FROM TransactionDetail TD
        //                                                  INNER JOIN Pack P ON TD.PackID = P.PackID
        //                                                  INNER JOIN Item I ON P.ItemID = I.ItemID
        //                                                  INNER JOIN PackTypeLanguage PTL ON P.PackTypeID = PTL.PackTypeID 
        //                                                  LEFT OUTER JOIN FixedTaxes FT ON P.PackID = FT.PackID
        //                                                  INNER JOIN PriceDefinition PD ON TD.PackID = PD.PacKID AND PriceListID = (SELECT KeyValue FROM Configuration WHERE KeyName = 'ConsumerPriceListID')

        //												  WHERE PTL.LanguageID = 1 AND TD.TransactionID = '{0}'
        //												  ", TranID);
        //                                        break;
        //                                }

        //                                InCubeQuery dtlQry = new InCubeQuery(db_vms, QueryString);
        //                                err = dtlQry.Execute();
        //                                err = dtlQry.FindFirst();

        //                                if (err != InCubeErrors.Success)
        //                                {
        //                                    throw new Exception("No details found");
        //                                }

        //                                while (err == InCubeErrors.Success)
        //                                {
        //                                    err = dtlQry.GetField("SalesTransactionTypeID", ref salesTransTypeID);
        //                                    err = dtlQry.GetField("ItemCode", ref ItemCode);
        //                                    err = dtlQry.GetField("Quantity", ref Quantity);
        //                                    err = dtlQry.GetField("UOM", ref UOM);
        //                                    err = dtlQry.GetField("Price", ref Price);
        //                                    err = dtlQry.GetField("Discount", ref Discount);
        //                                    err = dtlQry.GetField("Freight1Amt", ref freight1Amt);
        //                                    err = dtlQry.GetField("Freight2Amt", ref freight2Amt);
        //                                    err = dtlQry.GetField("Freight3Amt", ref freight3Amt);

        //                                    DataRow newInvoiceDetailsRow = ds.Tables["tblDetails"].NewRow();

        //                                    newInvoiceDetailsRow["ItemCode"] = ItemCode.ToString();
        //                                    newInvoiceDetailsRow["Qty"] = Quantity.ToString();
        //                                    newInvoiceDetailsRow["TaxCode"] = "X0";
        //                                    newInvoiceDetailsRow["UnitCode"] = UOM.ToString();
        //                                    newInvoiceDetailsRow["Freight1Code"] = freight1Code.ToString();
        //                                    newInvoiceDetailsRow["Freight2Code"] = freight2Code.ToString();
        //                                    newInvoiceDetailsRow["Freight3Code"] = freight3Code.ToString();

        //                                    decimal price = decimal.Parse(Price.ToString());
        //                                    decimal qty = decimal.Parse(Quantity.ToString());
        //                                    decimal discount = decimal.Parse(Discount.ToString());
        //                                    decimal tax1 = decimal.Parse(freight1Amt.ToString());
        //                                    decimal tax2 = decimal.Parse(freight2Amt.ToString());
        //                                    price -= (tax1 + tax2);
        //                                    decimal net = price * qty;
        //                                    newInvoiceDetailsRow["UnitPrice"] = price.ToString();
        //                                    newInvoiceDetailsRow["DicountPerc"] = (discount / net * 100).ToString();
        //                                    newInvoiceDetailsRow["TotalNetAmt"] = net.ToString();
        //                                    newInvoiceDetailsRow["Freight1Amt"] = (tax1 * qty).ToString();
        //                                    newInvoiceDetailsRow["Freight2Amt"] = (tax2 * qty).ToString();
        //                                    newInvoiceDetailsRow["Freight3Amt"] = freight3Amt.ToString();

        //                                    if (CreationReason.ToString() != "1")
        //                                    {
        //                                        newInvoiceDetailsRow["UnitPrice"] = "0";
        //                                        newInvoiceDetailsRow["TotalNetAmt"] = "0";
        //                                    }

        //                                    ds.Tables["tblDetails"].Rows.Add(newInvoiceDetailsRow);

        //                                    err = dtlQry.FindNext();
        //                                }
        //                                #endregion

        //                                #region(Send Invoice)
        //                                try
        //                                {
        //                                    ProxyService.HH_Add_InvoiceNoPayment(ds, mCompany, out errorMessage, out transId, out DocNum);
        //                                    WriteXML(ds, HDResult, LoggingFiles.errorInv, TranID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);

        //                                    if (errorMessage == "")
        //                                    {
        //                                        WriteMessage("\r\n" + TranID.ToString() + " - OK");

        //                                        QueryBuilderObject.SetField("Synchronized", "1");
        //                                        QueryBuilderObject.SetField("Notes", "'" + transId.ToString() + "'");
        //                                        err = QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TranID.ToString() + "'", db_vms);
        //                                        if (err != InCubeErrors.Success)
        //                                        {
        //                                            WriteMessage(", Failure in updating the sync flag, check InCubeLog.txt");
        //                                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), "Invoice sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        WriteMessage("\r\n" + TranID.ToString() + " - Rejected by SAP, check errorInv.log");
        //                                    }
        //                                }
        //                                catch (Exception sendEx)
        //                                {
        //                                    WriteMessage("\r\n" + TranID.ToString() + " - Failure in sending to SAP, check errorInv.log");
        //                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorInv);
        //                                }
        //                                #endregion
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                WriteMessage("\r\n" + TranID.ToString() + " - Failure in preparing data, check InCubeLog.txt");
        //                                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, TranID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
        //                            }
        //                            err = GetSalesTransactionInformation.FindNext();
        //                        }


        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                        result.Append(ex.Message);
        //                        if (res == Result.UnKnown)
        //                        {
        //                            res = Result.Failure;
        //                            WriteMessage("Unhandled exception !!");
        //                        }
        //                    }
        //                    finally
        //                    {
        //                        execManager.LogIntegrationEnding(processID, res, "", result.ToString());
        //                    }
        //                }

        //            }
        //            catch (Exception ex)
        //            {
        //                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
        //                WriteMessage("Fetching order failed !!");
        //            }
        //        }

        private void WriteXML(DataSet ds, bool success, LoggingFiles file, string fileName, string errorMessage)
        {
            try
            {
                if (!success)
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, fileName, errorMessage, LoggingType.Error, file);
                string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6) + "\\Backup\\" + DateTime.Today.ToString("yyyyMMdd");
                
                 
                switch (file)
                {
                    case LoggingFiles.errorInv:
                        path += "\\Invoices\\";
                        break;
                    case LoggingFiles.errorPay:
                        path += "\\Payments\\";
                        break;
                }

                if (success)
                    path += "Success\\";
                else
                    path += "Failure\\";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                ds.WriteXml(path + fileName + ".xml");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }

        private void WriteImportSAPScript(string content, string itemName)
        {
            try
            {
                string path =  System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) +"\\Backup\\" + DateTime.Today.ToString("yyyyMMdd") + "\\Import\\";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.AppendAllText(path + itemName + DateTime.Now.ToString(" - yyyyMMdd HHmmss") + ".sql", content);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #region SendReciepts
        public override void SendReciepts()
        {
            try
            {
                #region(Member Data)

                object PayID = "";
                object PayDate = "";
                object Customer = "";
                object Amount = "";
                object ChequeNO = "";
                object ChequeDate = "";
                object Bank = "";
                object PaymentType = "";
                object TransactionID = "";
                string BankCode = "";
                object employeeid = "";
                string CountryCode = "";
                object cheeq = "", cash = "";
                string account = "";
                bool HDResult, HDResultSp, TransIDSp;
                int transId;

                #endregion

                WriteMessage("\r\n" + "Sending Reciepts");

                #region(Build DataSet)

                //DataSet ds = new DataSet();
                //DataTable dtPaymentHeader = new DataTable("tblHeader");
                //dtPaymentHeader.Columns.Add("PaymentID");
                //dtPaymentHeader.Columns.Add("TransactionID");
                //dtPaymentHeader.Columns.Add("CustomerCode");
                //dtPaymentHeader.Columns.Add("PaymentDate", typeof(DateTime));
                //dtPaymentHeader.Columns.Add("PaymentType");
                //dtPaymentHeader.Columns.Add("Amount");
                //dtPaymentHeader.Columns.Add("VoucherNumber");
                //dtPaymentHeader.Columns.Add("VoucherDate", typeof(DateTime));
                //dtPaymentHeader.Columns.Add("BankCode");
                //dtPaymentHeader.Columns.Add("CountryCode");

                //ds.Tables.Add(dtPaymentHeader);
                DataSet ds = new DataSet();
                DataTable dtPaymentHeader = new DataTable("tblHeader");
                dtPaymentHeader.Columns.Add("PaymentID");
                dtPaymentHeader.Columns.Add("CustomerCode"); 
                dtPaymentHeader.Columns.Add("PaymentDate");
                dtPaymentHeader.Columns.Add("PaymentType");
                dtPaymentHeader.Columns.Add("AppliedAmount");
                dtPaymentHeader.Columns.Add("VoucherNumber");
                dtPaymentHeader.Columns.Add("VoucherDate");
                dtPaymentHeader.Columns.Add("BankCode");
                dtPaymentHeader.Columns.Add("CountryCode");
                dtPaymentHeader.Columns.Add("Account");
                dtPaymentHeader.Columns.Add("Currency");// Add Currency

                ds.Tables.Add(dtPaymentHeader);

                DataTable dtPaymentLine = new DataTable("tblDetails");
                dtPaymentLine.Columns.Add("InvoiceNumber");
                dtPaymentLine.Columns.Add("Amount");
                ds.Tables.Add(dtPaymentLine);
                #endregion

                #region(Fill Dataset)
                string QueryString = string.Format(@"SELECT 
                                                 CP.CustomerPaymentID, 
                                                 --CP.TransactionID,
                                                 CO.CustomerCode,
                                                 CP.PaymentDate, 
                                                 CASE CP.PaymentTypeID WHEN 1 THEN 1 ELSE 2 END PaymentTypeID, 
                                                 sum(CP.AppliedAmount)AppliedAmount,
                                                 CP.VoucherNumber, 
                                                 CP.VoucherDate, 
                                                 B.Code BankCode,
                                                  e.HourlyRegularRate cash,e.HourlyOvertimeRate cheeq
                                                 FROM CustomerPayment CP
                                                 INNER JOIN CustomerOutlet CO ON CP.CustomerID = CO.CustomerID AND CP.OutletID = CO.OutletID
                                                 INNER JOIN Employee E on CP.employeeid=e.employeeid
                                                 LEFT OUTER JOIN Bank B ON CP.BankID = B.BankID

                                                 WHERE CP.Synchronized = 0 AND CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3)
                                                 AND CP.PaymentDate >= CONVERT(datetime, '{0}/{1}/{2} 00:00:00', 102) 
                                                 AND CP.PaymentDate <= CONVERT(datetime, '{3}/{4}/{5} 23:59:59', 102)"
                    , Filters.FromDate.Year, Filters.FromDate.Month, Filters.FromDate.Day, Filters.ToDate.Year, Filters.ToDate.Month, Filters.ToDate.Day);

                if ( Filters.EmployeeID!= -1 )
                {
                    QueryString += " AND CP.EmployeeID = " + Filters.EmployeeID;
                }
                QueryString += " group by CP.CustomerPaymentID,CO.CustomerCode,CP.PaymentDate, CP.PaymentTypeID, CP.VoucherNumber, CP.VoucherDate, B.Code,e.HourlyRegularRate ,e.HourlyOvertimeRate ";

                InCubeQuery GetPaymentInformation = new InCubeQuery(db_vms, QueryString);

                err = GetPaymentInformation.Execute();
                err = GetPaymentInformation.FindFirst();
                while (err == InCubeErrors.Success)
                {
                    try
                    {
                        ds.Tables["tblHeader"].Clear();
                        ds.Tables["tblDetails"].Clear();

                        #region Get SalesTransaction Information
                        {
                            err = GetPaymentInformation.GetField("CustomerPaymentID", ref PayID);
                            err = GetPaymentInformation.GetField("employeeCode", ref employeeid);
                            err = GetPaymentInformation.GetField("CustomerCode", ref Customer);
                            err = GetPaymentInformation.GetField("PaymentDate", ref  PayDate);
                            err = GetPaymentInformation.GetField("PaymentTypeID", ref  PaymentType);
                            err = GetPaymentInformation.GetField("AppliedAmount", ref Amount);
                            err = GetPaymentInformation.GetField("VoucherNumber", ref ChequeNO);
                            err = GetPaymentInformation.GetField("VoucherDate", ref  ChequeDate);
                            err = GetPaymentInformation.GetField("BankCode", ref Bank);
                            err = GetPaymentInformation.GetField("cheeq", ref cheeq);
                            err = GetPaymentInformation.GetField("cash", ref cash);
                            int indexOfDash = Bank.ToString().IndexOf('-');
                            if (indexOfDash != -1)
                            {
                                CountryCode = Bank.ToString().Substring(0, indexOfDash);
                                BankCode = Bank.ToString().Substring(indexOfDash + 1, Bank.ToString().Length - indexOfDash - 1);
                            }
                        }
                        #endregion
                        account = cash.ToString();
                        DateTime _tranDate = DateTime.Parse(PayDate.ToString());

                        DataRow newPaymentHeaderRow = ds.Tables["tblHeader"].NewRow();

                        newPaymentHeaderRow["PaymentID"] = PayID.ToString();
                         newPaymentHeaderRow["CustomerCode"] = Customer.ToString();
                        newPaymentHeaderRow["PaymentDate"] = _tranDate.ToString("yyyy/MM/dd");
                        newPaymentHeaderRow["PaymentType"] = PaymentType.ToString();
                        newPaymentHeaderRow["AppliedAmount"] = Amount.ToString();

                        if (PaymentType.ToString() != "1")
                        {
                            DateTime _chequeDate = DateTime.Parse(ChequeDate.ToString());
                            newPaymentHeaderRow["VoucherNumber"] = ChequeNO.ToString();
                            newPaymentHeaderRow["VoucherDate"] = _chequeDate.ToString("yyyy/MM/dd");
                            newPaymentHeaderRow["BankCode"] = BankCode;
                            newPaymentHeaderRow["CountryCode"] = CountryCode;
                            account = cheeq.ToString();
                        }
                        else
                        {
                            newPaymentHeaderRow["VoucherNumber"] = "";
                            newPaymentHeaderRow["VoucherDate"] = DateTime.MinValue.ToString("yyyy/MM/dd");
                            newPaymentHeaderRow["BankCode"] = "";
                            newPaymentHeaderRow["CountryCode"] = "";
                        }
                        newPaymentHeaderRow["Account"] = account;
                        ds.Tables["tblHeader"].Rows.Add(newPaymentHeaderRow);

                        QueryString = @"SELECT 
                                                CP.TransactionID,
                                                AppliedAmount
                                                FROM CustomerPayment CP
                                                WHERE  CP.CustomerPaymentID='" + PayID.ToString() + "'    AND CP.PaymentStatusID <> 5 AND CP.PaymentTypeID IN (1,2,3)";
                        InCubeQuery GetPaymentDetail = new InCubeQuery(db_vms, QueryString);
                        err = GetPaymentDetail.Execute();
                        if (err == InCubeErrors.Success)
                        {
                            dtPaymentLine = GetPaymentDetail.GetDataTable();
                            for (int i = 0; i < dtPaymentLine.Rows.Count; i++)
                            {
                                DataRow newPaymentLineRow = ds.Tables["tblDetails"].NewRow();

                                TransactionID = dtPaymentLine.Rows[i]["TransactionID"].ToString();
                                Amount = dtPaymentLine.Rows[i]["AppliedAmount"].ToString();
                                newPaymentLineRow["InvoiceNumber"] = TransactionID.ToString();
                                newPaymentLineRow["Amount"] = Amount.ToString();
                                ds.Tables["tblDetails"].Rows.Add(newPaymentLineRow);

                            }

                        }
                        #region(Send Payment)
                        try
                        {
                            ProxyService.HH_Add_Payment_Invoice(ds, mCompany, out errorMessage, out transId);
                            WriteXML(ds,errorMessage=="", LoggingFiles.errorPay, PayID + " " + DateTime.Now.ToString("yyyyMMdd HHmmss"), errorMessage);
                            // ds.WriteXml(@"E:\ds.xml");
                            if (errorMessage == "")
                            {
                                WriteMessage("\r\n" + PayID.ToString() + " - OK");

                                QueryBuilderObject = new QueryBuilder();
                                QueryBuilderObject.SetField("Synchronized", "1");
                                err = QueryBuilderObject.UpdateQueryString("CustomerPayment", " CustomerPaymentID = '" + PayID.ToString() + "'", db_vms);
                                if (err != InCubeErrors.Success)
                                {
                                    WriteMessage(", Failure in updating the sync flag, check InCubeLog.txt");
                                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, PayID.ToString(), "Payment sent successfully to SAP but changing of synchronized flag failed", LoggingType.Error, LoggingFiles.InCubeLog);
                                }
                            }
                            else
                            {
                                WriteMessage("\r\n" + PayID.ToString() + " - Rejected by SAP, check errorPay.log");
                            }
                        }
                        catch (Exception sendEx)
                        {
                            WriteMessage("\r\n" + PayID.ToString() + " - Failure in sending to SAP, check errorPay.log");
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, PayID.ToString(), sendEx.StackTrace, LoggingType.Error, LoggingFiles.errorPay);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        WriteMessage("\r\n" + PayID.ToString() + " - Failure in preparing data, check InCubeLog.txt");
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, PayID.ToString(), ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                    err = GetPaymentInformation.FindNext();
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        #endregion


        #region UpdatePrice
        public override void UpdatePrice()
        {
            int TOTALUPDATED = 0;
            object field = new object();

            UpdatePriceList(ref TOTALUPDATED);

            WriteMessage("\r\n");
            WriteMessage("<<< PRICE >>> Total Updated = " + TOTALUPDATED);
        }
        private void UpdatePriceList(ref int TOTALUPDATED)
        {
            object field = new object();


            DataTable DT_PriceListHeader = ProxyService.HH_Get_PriceList_H(mCompany, out errorMessage);

            ClearProgress();
            SetProgressMax(DT_PriceListHeader.Rows.Count);

            foreach (DataRow row in DT_PriceListHeader.Rows)
            {
                ReportProgress( "Updating Price lists" );
          

                TOTALUPDATED++;

                //Price list Code	0
                //Price List Description 2
                //ItemCode	5
                //UOM	6
                //Price 7
                //TAX   8
                //Is default Price list	1
                //Customer group	3
                //Customer Code 4

                string PriceListCode = row["PLID"].ToString().Trim();
                string IsDefaultPricelist = row["PLIsDefault"].ToString().Trim();
                string PriceListDescription = row["PLDesc"].ToString().Trim();



                if (PriceListCode == string.Empty)
                    continue;



                string PriceListID = "1";

                err = ExistObject("PriceList", "PriceListCode", " PriceListCode = '" + PriceListCode + "'", db_vms);
                if (err == InCubeErrors.Success)
                {
                    PriceListID = GetFieldValue("PriceList", "PriceListID", " PriceListCode = '" + PriceListCode + "'", db_vms);

                    QueryBuilderObject.SetField("PriceListCode", "'" + PriceListCode + "'");
                    QueryBuilderObject.UpdateQueryString("PriceList", " PriceListID = " + PriceListID, db_vms);
                }
                else
                {
                    PriceListID = GetFieldValue("PriceList", "ISNULL(MAX(PriceListID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    QueryBuilderObject.SetField("PriceListCode", "'" + PriceListCode + "'");
                    QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString(DateFormat) + "'");
                    QueryBuilderObject.SetField("Priority", "1");
                    QueryBuilderObject.InsertQueryString("PriceList", db_vms);

                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + PriceListDescription + "'");
                    QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);

                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + PriceListDescription + "'");
                    QueryBuilderObject.InsertQueryString("PriceListLanguage", db_vms);
                }

                if (IsDefaultPricelist == "1")
                {
                    QueryBuilderObject.SetField("KeyValue", PriceListID);
                    QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                }

                err = ExistObject("PriceQuantityRange", "PriceQuantityRangeID", " PriceQuantityRangeID = 1", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("PriceQuantityRangeID", "1");
                    QueryBuilderObject.SetField("RangeStart", "1");
                    QueryBuilderObject.SetField("RangeEnd", "9999999");
                    QueryBuilderObject.InsertQueryString("PriceQuantityRange", db_vms);
                }
            }

            DataTable DT_PriceListDetails = ProxyService.HH_Get_PriceList_D(mCompany, out errorMessage);
            char[] arr = { '0' };
          
            foreach (DataRow row in DT_PriceListDetails.Rows)
            {
                string PLDID = GetFieldValue("PriceList", "PriceListID", "PriceListCode='"+ row["PricelistCode"].ToString().Trim()+"'",db_vms);
                string ItemCode = row["itemcode"].ToString().TrimStart(arr);
                string UOMdesc = "Õ»…";// row["UOM"].ToString().Trim();
                //if (UOMdesc != "Outer") continue;
                string Price = row["Price"].ToString().Trim();
                if (string.IsNullOrEmpty(Price)) Price = "0";
                string Tax = GetFieldValue("Item", "PackDefinition", "ItemCode= '"+ItemCode+"'",db_vms);
                if (Tax.Trim() == "") Tax = "0";

                 //Price = Math.Round(decimal.Parse(Price), 3).ToString();

                string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);

                if (ItemID == string.Empty)
                {
                    continue;
                }

                string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOMdesc + "'", db_vms);
                string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);

                int PriceDefinitionID = 1;

                string currentPrice = GetFieldValue("PriceDefinition", "Price", "PackID = " + PackID + " AND PriceListID = " + PLDID, db_vms);
                if (currentPrice.Equals(string.Empty))
                {
                    PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms));

                    QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                    QueryBuilderObject.SetField("QuantityRangeID", "1");
                    QueryBuilderObject.SetField("PackID", PackID);
                    QueryBuilderObject.SetField("CurrencyID", "1");
                    QueryBuilderObject.SetField("Tax", Tax);
                    QueryBuilderObject.SetField("Price", Price);
                    QueryBuilderObject.SetField("PriceListID", PLDID);
                    QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                }
                else
                {
                    PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PLDID, db_vms));

                    if (!currentPrice.Equals(Price.ToString()))
                    {
                        QueryBuilderObject.SetField("Tax", Tax);
                        QueryBuilderObject.SetField("Price", Price);
                        QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + PLDID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);
                    }
                }
                //if (!string.IsNullOrEmpty(Tax1) || !string.IsNullOrEmpty(Tax2))
                //{
                //    if (!decimal.Parse(Tax1).Equals(0) || !decimal.Parse(Tax2).Equals(0))
                //    {
                //        string currentPack = GetFieldValue("FixedTaxes", "PackID", "PackID = " + PackID, db_vms);
                //        if (currentPack.Equals(string.Empty))
                //        {
                //            QueryBuilderObject.SetField("PackID", PackID);
                //            QueryBuilderObject.SetField("PL102Tax", Tax1);
                //            QueryBuilderObject.SetField("PL42Tax", Tax2);
                //            QueryBuilderObject.InsertQueryString("FixedTaxes", db_vms);
                //        }
                //        else
                //        {
                //            QueryBuilderObject.SetField("PL102Tax", Tax1);
                //            QueryBuilderObject.SetField("PL42Tax", Tax2);
                //            QueryBuilderObject.UpdateQueryString("FixedTaxes", "PackID = " + PackID, db_vms);
                //        }
                //    }
                //}
            }

            DT_PriceListHeader.Dispose();
            DT_PriceListDetails.Dispose();
        }
        #endregion

        #region Bank
        public override void UpdateBank()
        {
            DataTable DT = new DataTable();
            InCubeErrors err;
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;
            try
            {
                DT = ProxyService.HH_Get_ExchangeRateInfo(mCompany, out errorMessage);



                if (errorMessage == string.Empty && DT != null && DT.Rows.Count > 0)
                {
                    ClearProgress();
                    SetProgressMax(DT.Rows.Count);

                    for (int rowIndex = 0; rowIndex < DT.Rows.Count; rowIndex++)
                    {

                        ReportProgress("Update Currencies ");
                        string CurrencyCode = DT.DefaultView[rowIndex]["CurrencyCode"].ToString().Trim();
                        string Rate = DT.DefaultView[rowIndex]["Rate"].ToString().Trim();
                         
                        if (string.IsNullOrEmpty(CurrencyCode))
                            continue;
                      
                        string CurrencyID = GetFieldValue("Currency", "CurrencyID", "Code = '" + CurrencyCode + "'", db_vms);
                        if (decimal.Parse(Rate) == 1) continue;
                        if (string.IsNullOrEmpty(CurrencyID.Trim()))
                        {
                            TOTALINSERTED++;
                            CurrencyID = GetFieldValue("Currency", "ISNULL(MAX(CurrencyID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("CurrencyID", CurrencyID);
                            QueryBuilderObject.SetField("Code", "'" + CurrencyCode + "'");
                            err = QueryBuilderObject.InsertQueryString("Currency", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("Currencyid", CurrencyID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + CurrencyCode + "'");
                            err = QueryBuilderObject.InsertQueryString("CurrencyLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("Currencyid", CurrencyID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "'" + CurrencyCode + "'");
                            err = QueryBuilderObject.InsertQueryString("CurrencyLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;
                        }
                        //else
                        //{
                        //    TOTALUPDATED++;
                        //    QueryBuilderObject.SetField("Description", "'" + BankName + "'");
                        //    err = QueryBuilderObject.UpdateQueryString("BankLanguage", "BankID = " + BankID, db_vms);
                        //    if (err != InCubeErrors.Success) continue;
                        //}

                        string oldRate = GetFieldValue("CurrencyRate", "Rate", string.Format("CurrencyID = {0}    order by RateDate desc", CurrencyID), db_vms);
                        if (string.IsNullOrEmpty(oldRate.Trim()) || decimal.Parse(oldRate)!=decimal.Parse(Rate))
                        {
                           string CurrencyRateID = GetFieldValue("CurrencyRate", "ISNULL(MAX(CurrencyRateID),0) + 1" , db_vms);

                            QueryBuilderObject.SetField("CurrencyRateID", CurrencyRateID);
                            QueryBuilderObject.SetField("CurrencyID", CurrencyID);
                            QueryBuilderObject.SetField("Rate", Rate);
                            QueryBuilderObject.SetField("RateDate", "getdate()");
                            err = QueryBuilderObject.InsertQueryString("CurrencyRate", db_vms);
                            if (err != InCubeErrors.Success) continue;
                        }
                    }
                }


                DT = ProxyService.HH_Get_Banks(mCompany, out errorMessage);

                if (errorMessage == string.Empty && DT != null && DT.Rows.Count > 0)
                {
                    ClearProgress();
                    SetProgressMax(DT.Rows.Count);

                    for (int rowIndex = 0; rowIndex < DT.Rows.Count; rowIndex++)
                    {

                        ReportProgress("Update Banks ");
                        string BankCode = DT.DefaultView[rowIndex]["Country"].ToString().Trim() + "-" + DT.DefaultView[rowIndex]["BankCode"].ToString().Trim();
                        string BankName = DT.DefaultView[rowIndex]["BankDescription"].ToString().Trim();
                        string BranchName = DT.DefaultView[rowIndex]["Branch"].ToString().Trim();

                        if (string.IsNullOrEmpty(BankCode))
                            continue;
                        if (BranchName.Trim() == "") BranchName = BankName;
                        string BankID = GetFieldValue("Bank", "BankID", "Code = '" + BankCode + "'", db_vms);

                        if (string.IsNullOrEmpty(BankID.Trim()))
                        {
                            TOTALINSERTED++;
                            BankID = GetFieldValue("Bank", "ISNULL(MAX(BankID),0) + 1", db_vms);
                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("Code", "'" + BankCode + "'");
                            err = QueryBuilderObject.InsertQueryString("Bank", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + BankName + "'");
                            err = QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "'" + BankName + "'");
                            err = QueryBuilderObject.InsertQueryString("BankLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;
                        }
                        else
                        {
                            TOTALUPDATED++;
                            QueryBuilderObject.SetField("Description", "'" + BankName + "'");
                            err = QueryBuilderObject.UpdateQueryString("BankLanguage", "BankID = " + BankID, db_vms);
                            if (err != InCubeErrors.Success) continue;
                        }

                        string BranchID = GetFieldValue("BankBranchLanguage", "BranchID", string.Format("BankID = {0} AND Description = '{1}' AND LanguageID = 1", BankID, BranchName), db_vms);
                        if (string.IsNullOrEmpty(BranchID.Trim()))
                        {
                            BranchID = GetFieldValue("BankBranch", "ISNULL(MAX(BranchID),0) + 1", " BankID = " + BankID, db_vms);

                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("BranchID", BranchID);
                            err = QueryBuilderObject.InsertQueryString("BankBranch", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("BranchID", BranchID);
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + BranchName + "'");
                            err = QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;

                            QueryBuilderObject.SetField("BankID", BankID);
                            QueryBuilderObject.SetField("BranchID", BranchID);
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "'" + BranchName + "'");
                            err = QueryBuilderObject.InsertQueryString("BankBranchLanguage", db_vms);
                            if (err != InCubeErrors.Success) continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMessage("Message: " + ex.Message + "\r\n" + "Data: " + ex.Data + "\r\n" + "HelpLink: " + ex.HelpLink + "\r\n" + "InnerException: " + ex.InnerException + "\r\n");
            }
            finally
            {
                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage(string.Format("<<< BANKS >>> Total Inserted = {0}, Total Updated = {1}", TOTALINSERTED, TOTALUPDATED));
            }
        }

        public override void UpdateInvoice()
        {
            DataTable DT = new DataTable();
            InCubeErrors err;
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;
            try
            {
                WriteMessage(" Get Invoices");
   
                   DT = ProxyService.HH_Get_DueInvoiceList(mCompany, out errorMessage);


                if (DT==null ||DT.Rows.Count ==0)
                { 
                        WriteMessage(" No data found !!");

                    return;
                }
                WriteMessage(" Rows retrieved: " + DT.Rows.Count);
                execManager.UpdateActionTotalRows(TriggerID, DT.Rows.Count);

                WriteMessage("\r\nSaving data to staging table ... ");

                Result res = SaveTable(DT, "Stg_Invoices");
                if (res != Result.Success)
                {
                    WriteMessage(" Error in saving to staging table !!");
                    return;
                }
                WriteMessage(" Success ..");
            }
            catch (Exception ex)
            {
                WriteMessage("Message: " + ex.Message + "\r\n" + "Data: " + ex.Data + "\r\n" + "HelpLink: " + ex.HelpLink + "\r\n" + "InnerException: " + ex.InnerException + "\r\n");
            }
            finally
            {
                DT.Dispose();
              }
        }
        #endregion
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
                dtData.Columns.Add("TriggerID", typeof(int));
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    dtData.Rows[i]["ID"] = (i + 1);
                    dtData.Rows[i]["TriggerID"] = TriggerID;
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

        #region UpdateStock

        public override void UpdateStock()
        {
            UpdateStockForWarehouse();
        }

        private void UpdateStockForWarehouse()
        {
            try
            {
                int TOTALUPDATED = 0;

                object field = new object();

                #region Update Stock

                DataTable DT_StoreBalance = ProxyService.HH_Get_Stores_Balance(mCompany, out errorMessage);

                ClearProgress();
                SetProgressMax(DT_StoreBalance.Rows.Count);

                string DeleteStock = "delete from WarehouseStock where  warehouseid in ((select VehicleID from RouteHistory where RouteHistoryID in (select max(RouteHistoryID) from RouteHistory group by RouteHistory.VehicleID) and RouteHistory.Uploaded=1))";
                QueryBuilderObject.RunQuery(DeleteStock, db_vms);


                foreach (DataRow row in DT_StoreBalance.Rows)
                { 
                    ReportProgress("Updating Stock");
                     

                    string WarehouseCode = row["warehousecode"].ToString().Trim();
                    string ItemCode = row["Itemcode"].ToString().Trim();
                    string UOMdesc = "Õ»…";// row["UOM"].ToString().Trim();
                    string Quantity = row["quantity"].ToString().Trim();
                    string Batch = "1990/01/01";
                    DateTime ExpiryDate = new DateTime(1990, 1, 1);

                    if (Convert.ToDecimal(Quantity) == 0)
                        continue;

                    if (WarehouseCode == string.Empty)
                        continue;

                    string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + WarehouseCode + "'", db_vms);
                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOMdesc + "'", db_vms);
                    string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);

                    if (WarehouseID == string.Empty)
                    {
                        continue;
                    }

                    if (ItemID == string.Empty || PackID == string.Empty)
                    {
                        continue;
                    }

                    TOTALUPDATED++;

                    string query = "Select PackID from Pack where ItemID = " + ItemID;
                    InCubeQuery CMD = new InCubeQuery(query, db_vms);
                    CMD.Execute();
                    err = CMD.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        CMD.GetField(0, ref field);
                        string _packid = field.ToString();

                        string _quantity = "0";

                        err = ExistObject("WarehouseStock", "PackID", string.Format("WarehouseID = {0} AND ZoneID = 1 AND PackID = {1} AND CONVERT(datetime, ExpiryDate, 103) = CONVERT(datetime, '{2}', 103) AND BatchNo = '{3}'", WarehouseID, _packid, ExpiryDate.ToString("dd/MM/yyyy"), Batch), db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                            QueryBuilderObject.SetField("ZoneID", "1");
                            QueryBuilderObject.SetField("PackID", _packid);
                            QueryBuilderObject.SetDateField("ExpiryDate", ExpiryDate);
                            QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");
                            QueryBuilderObject.SetField("SampleQuantity", "0");

                            if (_packid == PackID)
                            {
                                QueryBuilderObject.SetField("Quantity", Quantity);
                                QueryBuilderObject.SetField("BaseQuantity", Quantity);
                            }
                            else
                            {
                                QueryBuilderObject.SetField("Quantity", _quantity);
                                QueryBuilderObject.SetField("BaseQuantity", _quantity);
                            }

                            QueryBuilderObject.InsertQueryString("WarehouseStock", db_vms);
                        }
                        else if (_packid == PackID)
                        {
                            QueryBuilderObject.SetField("Quantity", Quantity);
                            QueryBuilderObject.SetField("BaseQuantity", Quantity);
                            QueryBuilderObject.UpdateQueryString("WarehouseStock", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                        }


                        err = CMD.FindNext();
                    }
                }

                DT_StoreBalance.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< STOCK Updated >>> Total Updated = " + TOTALUPDATED);
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog("IntegrationJTI", "UpdateStockForWarehouse", ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }

        }

        #endregion

        #region Update Discount

        public override void UpdateDiscount()
        {
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            object field = new object();

            InCubeQuery DeleteDiscountQuery = new InCubeQuery(db_vms, "Delete From Discount");
            DeleteDiscountQuery.ExecuteNonQuery();

            DataTable DT = new DataTable();
             
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
            DefaultOrganization();
            foreach (DataRow row in DT.Rows)
            { 

                ReportProgress("Updating Item Discounts ");
                 

                field = row[0];
                string CustomerCode = field.ToString();

                field = row[1];
                string ItemCode = field.ToString();
                string pack = row[2].ToString();
                string ItemID = GetFieldValue("Item", "ItemID", " ItemCode = '" + ItemCode + "' ", db_vms);

                string GroupID = GetFieldValue("CustomerGroupLanguage", "GroupID", " Description = '" + CustomerCode + "' ", db_vms);

                string PackTypeID = GetFieldValue("PackTypeLanguage", "top (1) PackTypeID", " Description = '" + pack + "' ", db_vms);
                string PackID = GetFieldValue("Pack", "PackID", "  ItemID = " + ItemID + " AND PackTypeID=" + PackTypeID, db_vms);


                field = row[3];
                decimal Discount = decimal.Parse(field.ToString());
                string MAXID = GetFieldValue("Discount", "DiscountID", "DiscountCode=" + ItemID == "" ? "'Group-" + CustomerCode + "'" : "'Itm-" + ItemCode + "'", db_vms);
                if (MAXID.Trim() == string.Empty)
                {
                    MAXID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);
                    string MAXDiscountAssignmentID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);


                    TOTALINSERTED++;

                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("AllItems", ItemID == "" ? "1" : "0");
                    QueryBuilderObject.SetField("PackID", ItemID == "" ? "null" : PackID);
                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    QueryBuilderObject.SetField("FOC", "0");
                    QueryBuilderObject.SetDateField("StartDate", DateTime.Now.Date);
                    QueryBuilderObject.SetDateField("EndDate", DateTime.Now.Date.AddYears(10));
                    QueryBuilderObject.SetField("DiscountTypeID", "1");
                    QueryBuilderObject.SetField("OrganizationID", OrganizationID.ToString());
                    QueryBuilderObject.SetField("DiscountCode", ItemID == "" ? "'Group-" + CustomerCode + "'" : "'Itm-" + ItemCode + "'");
                    QueryBuilderObject.SetField("TypeID", "1");
                    QueryBuilderObject.InsertQueryString("Discount", db_vms);


                    QueryBuilderObject.SetField("DiscountAssignmentID", MAXDiscountAssignmentID);
                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    if (GroupID == "")
                    {
                        QueryBuilderObject.SetField("AllCustomers", "1");
                    }
                    else
                    {
                        QueryBuilderObject.SetField("CustomerGroupID", GroupID);

                    }
                    QueryBuilderObject.InsertQueryString("DiscountAssignment", db_vms);

                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", ItemID == "" ? "'Group discount code(" + CustomerCode + ")'" : "'Item Discount code(" + ItemCode + ")'");
                    QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms);

                    QueryBuilderObject.SetField("DiscountID", MAXID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", ItemID == "" ? "'Group discount code(" + CustomerCode + ")'" : "'Item Discount code(" + ItemCode + ")'");
                    QueryBuilderObject.InsertQueryString("DiscountLanguage", db_vms);

                }
                else
                {

                    QueryBuilderObject.SetField("Discount", Discount.ToString());
                    QueryBuilderObject.UpdateQueryString("Discount", "DiscountID = " + MAXID, db_vms);

                }
            }

            //foreach (DataRow row in DT.Rows)
            //{
            //    IntegrationForm.progressBar1.Value++;
            //    IntegrationForm.lblProgress.Text = "Updating Discounts " + " " + IntegrationForm.progressBar1.Value + " / " + IntegrationForm.progressBar1.Maximum;
            //    Application.DoEvents();

            //    //Customer code	
            //    //Item Code	
            //    //Pack	
            //    //Discount 

            //    string CustomerCode = row[0].ToString().Trim();
            //    string ItemCode = row[1].ToString().Trim();
            //    string UOMdesc = row[2].ToString().Trim();
            //    string dis = row[3].ToString().Trim();

            //    if (CustomerCode == string.Empty)
            //        continue;

            //    string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + CustomerCode + "'", db_vms);
            //    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + CustomerCode + "'", db_vms);

            //    if (OutletID == string.Empty)
            //    {
            //        continue;
            //    }

            //    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode='" + ItemCode + "'", db_vms);

            //    if (ItemID == string.Empty)
            //    {
            //        continue;
            //    }

            //    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOMdesc + "'", db_vms);

            //    if (PackTypeID == string.Empty)
            //    {
            //        continue;
            //    }

            //    string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);

            //    if (PackID == string.Empty)
            //    {
            //        continue;
            //    }

            //    decimal Discount = decimal.Parse(dis);

            //    string MAXID = GetFieldValue("Discount", " IsNull(MAX(DiscountID),0) + 1 ", db_vms);

            //    err = ExistObject("Discount", "Discount", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //    if (err == InCubeErrors.Success)
            //    {
            //        TOTALUPDATED++;

            //        string DiscountID = GetFieldValue("Discount", "DiscountID", " CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //        QueryBuilderObject.SetField("Discount", Discount.ToString());
            //        QueryBuilderObject.UpdateQueryString("Discount", "DiscountID = " + DiscountID, db_vms);
            //    }
            //    else
            //    {
            //        TOTALINSERTED++;

            //        QueryBuilderObject.SetField("DiscountID", MAXID);
            //        QueryBuilderObject.SetField("PackID", PackID);
            //        QueryBuilderObject.SetField("CustomerID", CustomerID);
            //        QueryBuilderObject.SetField("OutletID", OutletID);
            //        QueryBuilderObject.SetField("Discount", Discount.ToString());
            //        QueryBuilderObject.SetField("FOC", "0");
            //        QueryBuilderObject.SetField("StartDate", "'" + DateTime.Now.Date.ToString("dd/MMM/yyyy") + "'");
            //        QueryBuilderObject.SetField("EndDate", "'" + DateTime.Now.Date.AddYears(10).ToString("dd/MMM/yyyy") + "'");
            //        QueryBuilderObject.InsertQueryString("Discount", db_vms);
            //    }
            //}

            DT.Dispose();
            //DA.Dispose();
            WriteMessage("\r\n");
            WriteMessage("<<< CUSTOMERS DISCOUNT >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
        }

        #endregion


        #region Update Route

        public override void UpdateRoutes()
        {
            int num = 0;
            object obj2 = new object();
            this.DefaultOrganization();
            DataTable dataTable = new DataTable(); 
            ClearProgress();
            SetProgressMax(dataTable.Rows.Count);
            foreach (DataRow row in dataTable.Rows)
            { 
                ReportProgress( "Updating Routes "); 
                string str = row[0].ToString().Trim();
                string str2 = row[1].ToString().Trim();
                string str3 = row[2].ToString().Trim();
                string str4 = row[3].ToString().Trim();
                string fieldValue = row[4].ToString().Trim();
                if (fieldValue == "1")
                {
                }
                string str6 = row[5].ToString().Trim();
                if (str6 == "1")
                {
                }
                string str7 = row[6].ToString().Trim();
                if (str7 == "1")
                {
                }
                string str8 = row[7].ToString().Trim();
                if (str8 == "1")
                {
                }
                string str9 = row[8].ToString().Trim();
                if (str9 == "1")
                {
                }
                string str10 = row[9].ToString().Trim();
                if (str10 == "1")
                {
                }
                string str11 = row[10].ToString().Trim();
                if (str11 == "1")
                {
                }
                string organizationID = row[11].ToString().Trim();
                if (organizationID == string.Empty)
                {
                    organizationID = this.OrganizationID.ToString();
                }
                if (str2 != string.Empty)
                {
                    string str13 = base.GetFieldValue("TerritoryLanguage", "TerritoryID", " Description = '" + str + "' AND LanguageID = 1", base.db_vms);
                    if (str13 == string.Empty)
                    {
                        str13 = base.GetFieldValue("Territory", "ISNULL(MAX(TerritoryID),0) + 1", base.db_vms);
                    }
                    string str14 = base.GetFieldValue("CustomerOutlet", "CustomerID", " CustomerCode = '" + str4 + "'", base.db_vms);
                    string str15 = base.GetFieldValue("CustomerOutlet", "OutletID", " CustomerCode = '" + str4 + "'", base.db_vms);
                    string str16 = base.GetFieldValue("Employee", "EmployeeID", " EmployeeCode = '" + str3 + "'", base.db_vms);
                    if (base.ExistObject("Territory", "TerritoryID", "TerritoryID = " + str13, base.db_vms) != InCubeErrors.Success)
                    {
                        num++;
                        this.QueryBuilderObject.SetField("TerritoryID", str13);
                        this.QueryBuilderObject.SetField("OrganizationID", organizationID);
                        this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.InsertQueryString("Territory", base.db_vms);
                    }
                    if (base.ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + str13 + " AND LanguageID = 1", base.db_vms) != InCubeErrors.Success)
                    {
                        this.QueryBuilderObject.SetField("TerritoryID", str13);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str + "'");
                        this.QueryBuilderObject.InsertQueryString("TerritoryLanguage", base.db_vms);
                    }
                    if (base.ExistObject("TerritoryLanguage", "TerritoryID", "TerritoryID = " + str13 + " AND LanguageID = 2", base.db_vms) != InCubeErrors.Success)
                    {
                        this.QueryBuilderObject.SetField("TerritoryID", str13);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "'" + str + "'");
                        this.QueryBuilderObject.InsertQueryString("TerritoryLanguage", base.db_vms);
                    }
                    string str17 = str2;
                    string str18 = base.GetFieldValue("RouteLanguage", "RouteID", " Description = '" + str2 + "' AND LanguageID = 1", base.db_vms);
                    if (str18 == string.Empty)
                    {
                        str18 = base.GetFieldValue("Route", "ISNULL(MAX(RouteID),0) + 1", base.db_vms);
                    }
                    if (base.ExistObject("Route", "RouteID", "RouteID = " + str18, base.db_vms) != InCubeErrors.Success)
                    {
                        DateTime time = DateTime.Parse(DateTime.Now.Date.AddHours(7.0).ToString());
                        DateTime time2 = DateTime.Parse(DateTime.Now.Date.AddHours(23.0).ToString());
                        this.QueryBuilderObject.SetField("RouteID", str18);
                        this.QueryBuilderObject.SetField("Inactive", "0");
                        this.QueryBuilderObject.SetField("TerritoryID", str13);
                        this.QueryBuilderObject.SetField("EstimatedStart", "'" + time.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("EstimatedEnd", "'" + time2.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("CreatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.SetField("UpdatedBy", this.UserID.ToString());
                        this.QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(this.DateFormat) + "'");
                        this.QueryBuilderObject.InsertQueryString("Route", base.db_vms);
                    }
                    if (base.ExistObject("RouteLanguage", "RouteID", "RouteID = " + str18 + " AND LanguageID = 1", base.db_vms) != InCubeErrors.Success)
                    {
                        this.QueryBuilderObject.SetField("RouteID", str18);
                        this.QueryBuilderObject.SetField("LanguageID", "1");
                        this.QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                        this.QueryBuilderObject.InsertQueryString("RouteLanguage", base.db_vms);
                    }
                    if (base.ExistObject("RouteLanguage", "RouteID", "RouteID = " + str18 + " AND LanguageID = 2", base.db_vms) != InCubeErrors.Success)
                    {
                        this.QueryBuilderObject.SetField("RouteID", str18);
                        this.QueryBuilderObject.SetField("LanguageID", "2");
                        this.QueryBuilderObject.SetField("Description", "'" + str2 + "'");
                        this.QueryBuilderObject.InsertQueryString("RouteLanguage", base.db_vms);
                    }
                    if (base.ExistObject("RouteVisitPattern", "RouteID", "RouteID = " + str18, base.db_vms) != InCubeErrors.Success)
                    {
                        num++;
                        this.QueryBuilderObject.SetField("RouteID", str18);
                        this.QueryBuilderObject.SetField("Week", "1");
                        this.QueryBuilderObject.SetField("Saturday", fieldValue);
                        this.QueryBuilderObject.SetField("Sunday", str6);
                        this.QueryBuilderObject.SetField("Monday", str7);
                        this.QueryBuilderObject.SetField("Tuesday", str8);
                        this.QueryBuilderObject.SetField("Wednesday", str9);
                        this.QueryBuilderObject.SetField("Thursday", str10);
                        this.QueryBuilderObject.SetField("Friday", str11);
                        this.QueryBuilderObject.InsertQueryString("RouteVisitPattern", base.db_vms);
                    }
                    if (str15 != string.Empty)
                    {
                        if (base.ExistObject("RouteCustomer", "RouteID", "RouteID = " + str18 + " AND CustomerID = " + str14 + " AND OutletID=" + str15, base.db_vms) != InCubeErrors.Success)
                        {
                            num++;
                            this.QueryBuilderObject.SetField("RouteID", str18);
                            this.QueryBuilderObject.SetField("CustomerID", str14);
                            this.QueryBuilderObject.SetField("OutletID", str15);
                            this.QueryBuilderObject.InsertQueryString("RouteCustomer", base.db_vms);
                        }
                        if (base.ExistObject("CustOutTerritory", "TerritoryID", "TerritoryID = " + str13 + " AND CustomerID = " + str14 + " AND OutletID = " + str15, base.db_vms) != InCubeErrors.Success)
                        {
                            num++;
                            this.QueryBuilderObject.SetField("CustomerID", str14);
                            this.QueryBuilderObject.SetField("OutletID", str15);
                            this.QueryBuilderObject.SetField("TerritoryID", str13);
                            this.QueryBuilderObject.InsertQueryString("CustOutTerritory", base.db_vms);
                        }
                    }
                    if (((str16 != string.Empty) && (base.ExistObject("Employee", "EmployeeID", "EmployeeID = " + str16, base.db_vms) == InCubeErrors.Success)) && (base.ExistObject("EmployeeTerritory", "EmployeeID", "EmployeeID = " + str16 + " AND TerritoryID = " + str13, base.db_vms) != InCubeErrors.Success))
                    {
                        num++;
                        this.QueryBuilderObject.SetField("EmployeeID", str16);
                        this.QueryBuilderObject.SetField("TerritoryID", str13);
                        this.QueryBuilderObject.InsertQueryString("EmployeeTerritory", base.db_vms);
                    }
                }
            }
            dataTable.Dispose();
            //adapter.Dispose();
            base.WriteMessage("\r\n");
            base.WriteMessage("<<< ROUTE >>> Total Inserted = " + num);
        }


        #endregion

        #region SendInvoices

        #endregion

        #region SendReturns


        #endregion

        #region SendReciepts

        #endregion

        #region SendTransfers

        #endregion

        private void DefaultOrganization()
        {
            OrganizationID =1;
            if (OrganizationID == 0)
            {
                OrganizationID = 1;
                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.Date.ToString(DateFormat) + "'");
                QueryBuilderObject.InsertQueryString("Organization", db_vms);

                QueryBuilderObject.SetField("OrganizationID", "1");
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'Default Organization'");
                QueryBuilderObject.InsertQueryString("OrganizationLanguage", db_vms);
            }
        }

        #region UpdateCustomer

        private string InsertUpdateGeographicalLocations(string country, string state, string city, string area, string street)
        {
            string streetID = "0";
            try
            {
                if (country.Equals(string.Empty))
                    return "0";

                string countryID = GetFieldValue("CountryLanguage", "CountryID", "Description = '" + country + "'", db_vms, dbTrans);

                if (countryID.Equals(string.Empty))
                {
                    countryID = GetFieldValue("Country", "ISNULL(MAX(CountryID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    err = QueryBuilderObject.InsertQueryString("Country", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting country [" + country + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", country);
                    err = QueryBuilderObject.InsertQueryString("CountryLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for country [" + country + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", country);
                    err = QueryBuilderObject.InsertQueryString("CountryLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for country [" + country + "] failed");
                        return string.Empty;
                    }
                }

                if (state.Equals(string.Empty))
                    return "0";

                string stateID = GetFieldValue("StateLanguage", "StateID", "Description = '" + state + "'", db_vms, dbTrans);

                if (stateID.Equals(string.Empty))
                {
                    stateID = GetFieldValue("State", "ISNULL(MAX(StateID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    err = QueryBuilderObject.InsertQueryString("State", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting state [" + state + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", state);
                    err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for state [" + state + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", state);
                    err = QueryBuilderObject.InsertQueryString("StateLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for state [" + state + "] failed");
                        return string.Empty;
                    }
                }

                if (city.Equals(string.Empty))
                    return "0";

                string cityID = GetFieldValue("CityLanguage", "CityID", "Description = '" + city + "'", db_vms, dbTrans);

                if (cityID.Equals(string.Empty))
                {
                    cityID = GetFieldValue("City", "ISNULL(MAX(CityID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    err = QueryBuilderObject.InsertQueryString("City", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting city [" + city + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", city);
                    err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for city [" + city + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", city);
                    err = QueryBuilderObject.InsertQueryString("CityLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for city [" + city + "] failed");
                        return string.Empty;
                    }
                }

                if (area.Equals(string.Empty))
                    return "0";

                string areaID = GetFieldValue("AreaLanguage", "AreaID", "Description = '" + area + "'", db_vms, dbTrans);

                if (areaID.Equals(string.Empty))
                {
                    areaID = GetFieldValue("Area", "ISNULL(MAX(AreaID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    err = QueryBuilderObject.InsertQueryString("Area", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting area [" + area + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", area);
                    err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for area [" + area + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", area);
                    err = QueryBuilderObject.InsertQueryString("AreaLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for area [" + area + "] failed");
                        return string.Empty;
                    }
                }

                if (street.Equals(string.Empty))
                    return "0";

                streetID = GetFieldValue("StreetLanguage", "StreetID", "Description = '" + street + "'", db_vms, dbTrans);

                if (streetID.Equals(string.Empty))
                {
                    streetID = GetFieldValue("Street", "ISNULL(MAX(StreetID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    err = QueryBuilderObject.InsertQueryString("Street", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting street [" + street + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", street);
                    err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for street [" + street + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CountryID", countryID);
                    QueryBuilderObject.SetStringField("StateID", stateID);
                    QueryBuilderObject.SetStringField("CityID", cityID);
                    QueryBuilderObject.SetStringField("AreaID", areaID);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", street);
                    err = QueryBuilderObject.InsertQueryString("StreetLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for street [" + street + "] failed");
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMessage("Adding locations: " + country + ", " + state + ", " + city + ", " + area + ", " + street + " failed");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return streetID;
        }
        private string InsertUpdateMasterCustomer(string customerCode, string phoneNo, string fax, string email, string customerDescEn, string customerAddressEn, string customerDescAr, string streetID, string customerAddressAr, string balance, string creditLimit, string onHold, string orgID, bool UpdateMasterData, string CommitmentLimit, ref string accountID)
        {
            string customerID = string.Empty;
            accountID = string.Empty;
            try
            {
                #region(Header)

                if (customerCode == string.Empty)
                    return string.Empty;

                customerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + customerCode + "'", db_vms, dbTrans);
                bool newCustomer = customerID == string.Empty;

                if (newCustomer)
                {
                    customerID = GetFieldValue("Customer", "ISNULL(MAX(CustomerID),0)+1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();

                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("Phone", phoneNo);
                    QueryBuilderObject.SetStringField("Fax", fax);
                    QueryBuilderObject.SetStringField("Email", email);
                    QueryBuilderObject.SetStringField("CustomerCode", customerCode);
                    QueryBuilderObject.SetStringField("OnHold", onHold);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("StreetAddress", "0");
                    QueryBuilderObject.SetStringField("Inactive", "0");
                    QueryBuilderObject.SetStringField("New", "0");
                    QueryBuilderObject.SetStringField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                    QueryBuilderObject.SetStringField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                    err = QueryBuilderObject.InsertQueryString("Customer", db_vms, dbTrans);

                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else if (UpdateMasterData)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Phone", phoneNo);
                    QueryBuilderObject.SetStringField("Fax", fax);
                    QueryBuilderObject.SetStringField("Email", email);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("OnHold", onHold);
                    QueryBuilderObject.SetStringField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                    err = QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + customerID, db_vms, dbTrans);

                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion

                #region(Description)

                if (!newCustomer)
                    exist = ExistObject("CustomerLanguage", "Description", string.Format("CustomerID = {0} AND LanguageID = {1}", customerID, 1), db_vms, dbTrans) == InCubeErrors.Success;
                if (newCustomer || !exist)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", customerDescEn);
                    QueryBuilderObject.SetStringField("Address", customerAddressEn);
                    err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else if (UpdateMasterData)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Description", customerDescEn);
                    QueryBuilderObject.SetStringField("Address", customerAddressEn);
                    err = QueryBuilderObject.UpdateQueryString("CustomerLanguage", string.Format("CustomerID = {0} AND LanguageID = {1}", customerID, 1), db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating english description for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                if (!newCustomer)
                    exist = ExistObject("CustomerLanguage", "Description", string.Format("CustomerID = {0} AND LanguageID = {1}", customerID, 2), db_vms, dbTrans) == InCubeErrors.Success;
                if (newCustomer || !exist)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", customerDescAr);
                    QueryBuilderObject.SetStringField("Address", customerAddressAr);
                    err = QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else if (UpdateMasterData)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Description", customerDescAr);
                    QueryBuilderObject.SetStringField("Address", customerAddressAr);
                    err = QueryBuilderObject.UpdateQueryString("CustomerLanguage", string.Format("CustomerID = {0} AND LanguageID = {1}", customerID, 2), db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating arabic description for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                #endregion

                #region(Account)

                if (!newCustomer)
                    accountID = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + customerID, db_vms, dbTrans);
                if (accountID != string.Empty)
                {
                    if (UpdateMasterData)
                    {
                        QueryBuilderObject.SetStringField("CommitmentLimit", CommitmentLimit);
                        QueryBuilderObject.SetStringField("CreditLimit", creditLimit);
                        QueryBuilderObject.SetStringField("Balance", balance);
                        err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + accountID, db_vms, dbTrans);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("\r\nUpdating account for master customer [" + customerCode + "] failed");
                            return string.Empty;
                        }
                    }
                }
                else
                {
                    accountID = GetFieldValue("Account", "ISNULL(MAX(AccountID),0) + 1", db_vms, dbTrans);
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", accountID);
                    QueryBuilderObject.SetStringField("CommitmentLimit", CommitmentLimit);
                    QueryBuilderObject.SetStringField("AccountTypeID", "1");
                    QueryBuilderObject.SetStringField("CreditLimit", creditLimit);
                    QueryBuilderObject.SetStringField("Balance", balance);
                    QueryBuilderObject.SetStringField("GL", "0");
                    QueryBuilderObject.SetStringField("OrganizationID", orgID);
                    QueryBuilderObject.SetStringField("CurrencyID", "1");
                    err = QueryBuilderObject.InsertQueryString("Account", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nAdding account for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("AccountID", accountID);
                    err = QueryBuilderObject.InsertQueryString("AccountCust", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nLinking account for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", accountID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", customerDescEn.Trim() + " Account");
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for account for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", accountID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", customerDescAr.Trim() + " Õ”«»");
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for account for master customer [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nAdding master customer [" + customerCode + "] info failed");
                return string.Empty;
            }
            return customerID;
        }
        private string InsertUpdateCustomerGroup(string groupCode, string groupDescEn, string groupDescAr)
        {
            string customerGroupID = "-1";
            try
            {
                #region (Header)

                if (groupCode == string.Empty)
                    return "-1";

                customerGroupID = GetFieldValue("CustomerGroup", "GroupID", "GroupCode = '" + groupCode + "'", db_vms, dbTrans);
                bool newCustomerGroup = customerGroupID == string.Empty;

                if (newCustomerGroup)
                {
                    customerGroupID = GetFieldValue("CustomerGroup", "ISNULL(MAX(GroupID),0)+1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("GroupID", customerGroupID);
                    QueryBuilderObject.SetStringField("GroupCode", groupCode);
                    err = QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting customer group [" + groupCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion

                #region(Description)

                string savedDescEn = string.Empty;
                 
                if (newCustomerGroup || ExistObject("CustomerGroupLanguage", "Description", string.Format("GroupID = {0} AND LanguageID = {1}", customerGroupID, 1), db_vms, dbTrans)!=InCubeErrors.Success)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("GroupID", customerGroupID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", groupDescEn);
                    err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for customer group [" + groupCode + "] failed");
                        return string.Empty;
                    }
                }
                //else  
                //{
                //    QueryBuilderObject = new QueryBuilder();
                //    QueryBuilderObject.SetStringField("Description", groupDescEn);
                //    err = QueryBuilderObject.UpdateQueryString("CustomerGroupLanguage", string.Format("GroupID = {0} AND LanguageID = {1}", customerGroupID, 1), db_vms, dbTrans);
                //    if (err != InCubeErrors.Success)
                //    {
                //        WriteMessage("\r\nUpdating english description for customer group [" + groupCode + "] failed");
                //        return string.Empty;
                //    }
                //}

                string savedDescAr = string.Empty;
              
                if (newCustomerGroup || ExistObject("CustomerGroupLanguage", "Description", string.Format("GroupID = {0} AND LanguageID = {1}", customerGroupID, 2), db_vms, dbTrans)!= InCubeErrors.Success)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("GroupID", customerGroupID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", groupDescAr);
                    err = QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for customer group [" + groupCode + "] failed");
                        return string.Empty;
                    }
                }
                //else  
                //{
                //    QueryBuilderObject = new QueryBuilder();
                //    QueryBuilderObject.SetStringField("Description", groupDescAr);
                //    err = QueryBuilderObject.UpdateQueryString("CustomerGroupLanguage", string.Format("GroupID = {0} AND LanguageID = {1}", customerGroupID, 2), db_vms, dbTrans);
                //    if (err != InCubeErrors.Success)
                //    {
                //        WriteMessage("\r\nUpdating arabic description for customer group [" + groupCode + "] failed");
                //        return string.Empty;
                //    }
                //}

                #endregion
            }
            catch (Exception ex)
            {
                WriteMessage("Adding customer group [" + groupCode + "] failed");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return customerGroupID;
        }
        private string InsertUpdatePaymentTerm(string paymentTerm)
        {
            string paymentTermID = "0";
            int period = 0;
            try
            {
                if (!int.TryParse(paymentTerm, out period) || period == 0)
                    return "0";

                paymentTermID = GetFieldValue("PaymentTerm", "PaymentTermID", "SimplePeriodWidth = " + period, db_vms, dbTrans);

                if (paymentTermID == string.Empty)
                {
                    paymentTermID = GetFieldValue("PaymentTerm", "ISNULL(MAX(PaymentTermID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                    QueryBuilderObject.SetStringField("PaymentTermTypeID", "1");
                    QueryBuilderObject.SetStringField("SimplePeriodWidth", period.ToString());
                    QueryBuilderObject.SetStringField("SimplePeriodID", "1");
                    err = QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting payment term for [" + period + "] days failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", "Every " + period + " Days");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for payment term for [" + period + "] days failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", "ﬂ· " + period + " √Ì«„");
                    err = QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for payment term for [" + period + "] days failed");
                        return string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteMessage("Adding payment term for [" + period + "] days failed");
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                return string.Empty;
            }
            return paymentTermID;
        }
        private string InsertUpdateCustomerOutlet(string customerCode, string masterCustomerID, string phoneNo, string faxNo, string email, string customerTypeID, string onHold, string taxable, string barCode, string paymentTermID, string outletDescEn, string outletDescAr, string outletAddressEn, string outletAddressAr, string streetID, string gpsLongitude, string gpsLatitude, bool UpdateBalances, string creditLimit, string balance, string customerAccountID, string orgID, string customerGroupID, string CommitmentLimit, ref bool newOutlet, ref string outletID, ref string outletAccountID)
        {
            string customerID = string.Empty;
            outletAccountID = string.Empty;
            outletID = string.Empty;
            newOutlet = false;
            try
            {
                #region(Header)

                if (customerCode == string.Empty)
                    return string.Empty;

                customerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + customerCode + "'", db_vms, dbTrans);
                if (customerID != string.Empty)
                {
                    outletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + customerCode + "'", db_vms, dbTrans);
                }
                else
                {
                    customerID = masterCustomerID;
                    outletID = GetFieldValue("CustomerOutlet", "ISNULL(MAX(OutletID),0)+1", "CustomerID = " + customerID, db_vms, dbTrans);
                    newOutlet = true;
                }

                if (!newOutlet)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Phone", phoneNo);
                    QueryBuilderObject.SetStringField("Fax", faxNo);
                    QueryBuilderObject.SetStringField("Email", email);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("CustomerTypeID", customerTypeID);
                    QueryBuilderObject.SetStringField("OnHold", onHold);
                    QueryBuilderObject.SetStringField("Taxeable", taxable);
                    QueryBuilderObject.SetStringField("OrganizationID", orgID);
                    QueryBuilderObject.SetStringField("Barcode", barCode);
                    QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                    QueryBuilderObject.SetStringField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                    err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "CustomerID = " + customerID + " AND OutletID = " + outletID, db_vms, dbTrans);

                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("OutletID", outletID);
                    QueryBuilderObject.SetStringField("CustomerCode", customerCode);
                    QueryBuilderObject.SetStringField("Barcode", barCode);
                    QueryBuilderObject.SetStringField("Phone", phoneNo);
                    QueryBuilderObject.SetStringField("Fax", faxNo);
                    QueryBuilderObject.SetStringField("Email", email);
                    QueryBuilderObject.SetStringField("StreetID", streetID);
                    QueryBuilderObject.SetStringField("GPSLatitude", gpsLongitude);
                    QueryBuilderObject.SetStringField("GPSLongitude", gpsLatitude);
                    QueryBuilderObject.SetStringField("Taxeable", taxable);
                    QueryBuilderObject.SetStringField("CustomerTypeID", customerTypeID);
                    QueryBuilderObject.SetStringField("CurrencyID", "1");
                    QueryBuilderObject.SetStringField("OnHold", onHold);
                    QueryBuilderObject.SetStringField("StreetAddress", "0");
                    QueryBuilderObject.SetStringField("InActive", "0");
                    QueryBuilderObject.SetStringField("Notes", "0");
                    QueryBuilderObject.SetStringField("SkipCreditCheck", "0");
                    QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                    QueryBuilderObject.SetStringField("OrganizationID", orgID);
                    QueryBuilderObject.SetStringField("CreatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                    QueryBuilderObject.SetStringField("UpdatedBy", UserID.ToString());
                    QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);

                    err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms, dbTrans);

                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion

                #region(Description)

                if (!newOutlet)
                    exist = ExistObject("CustomerOutletLanguage", "Description", string.Format("CustomerID = {0} AND OutletID = {1} AND LanguageID = {2}", customerID, outletID, 1), db_vms, dbTrans) == InCubeErrors.Success;
                if (newOutlet || !exist)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("OutletID", outletID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", outletDescEn);
                    QueryBuilderObject.SetStringField("Address", outletAddressEn);
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Description", outletDescEn);
                    QueryBuilderObject.SetStringField("Address", outletAddressEn);
                    err = QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Format("CustomerID = {0} AND OutletID = {1} AND LanguageID = {2}", customerID, outletID, 1), db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating english description for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                if (!newOutlet)
                    exist = ExistObject("CustomerOutletLanguage", "Description", string.Format("CustomerID = {0} AND OutletID = {1} AND LanguageID = {2}", customerID, outletID, 2), db_vms, dbTrans) == InCubeErrors.Success;
                if (newOutlet || !exist)
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("OutletID", outletID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", outletDescAr);
                    QueryBuilderObject.SetStringField("Address", outletAddressAr);
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("Description", outletDescAr);
                    QueryBuilderObject.SetStringField("Address", outletAddressAr);
                    err = QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", string.Format("CustomerID = {0} AND OutletID = {1} AND LanguageID = {2}", customerID, outletID, 2), db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating arabic description for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion

                #region(Account)

                if (!newOutlet)
                    outletAccountID = GetFieldValue("AccountCustOut", "AccountID", string.Format("CustomerID = {0} AND OutletID = {1}", customerID, outletID), db_vms, dbTrans);
                if (outletAccountID != string.Empty)
                {
                    QueryBuilderObject.SetField("ParentAccountID", customerAccountID);
                    if (UpdateBalances)
                    {

                        QueryBuilderObject.SetStringField("CommitmentLimit", CommitmentLimit);
                        QueryBuilderObject.SetStringField("CreditLimit", creditLimit);
                        QueryBuilderObject.SetStringField("Balance", balance);
                    }
                    err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + outletAccountID, db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nUpdating account for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }
                else
                {
                    outletAccountID = GetFieldValue("Account", "ISNULL(MAX(AccountID),0) + 1", db_vms, dbTrans);

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", outletAccountID);
                    QueryBuilderObject.SetStringField("AccountTypeID", "1");
                    QueryBuilderObject.SetStringField("CommitmentLimit", CommitmentLimit);
                    QueryBuilderObject.SetStringField("CreditLimit", creditLimit);
                    QueryBuilderObject.SetStringField("Balance", balance);
                    QueryBuilderObject.SetStringField("GL", "0");
                    QueryBuilderObject.SetStringField("OrganizationID", orgID);
                    QueryBuilderObject.SetStringField("CurrencyID", "1");
                    QueryBuilderObject.SetField("ParentAccountID", customerAccountID);
                    err = QueryBuilderObject.InsertQueryString("Account", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nAdding account for customer outlet [" + customerID + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("CustomerID", customerID);
                    QueryBuilderObject.SetStringField("OutletID", outletID);
                    QueryBuilderObject.SetStringField("AccountID", outletAccountID);
                    err = QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nLinking account for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", outletAccountID);
                    QueryBuilderObject.SetStringField("LanguageID", "1");
                    QueryBuilderObject.SetStringField("Description", outletDescEn.Trim() + " Account");
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting english description for account for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetStringField("AccountID", outletAccountID);
                    QueryBuilderObject.SetStringField("LanguageID", "2");
                    QueryBuilderObject.SetStringField("Description", "Õ”«» " + outletDescAr.Trim());
                    err = QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nInserting arabic description for account for customer outlet [" + customerCode + "] failed");
                        return string.Empty;
                    }
                }

                #endregion

                #region(Customer Group)

                if (customerGroupID != "-1")
                {
                    incubeQuery = new InCubeQuery(string.Format("DELETE FROM CustomerOutletGroup WHERE CustomerID = {0} AND OutletID = {1}", customerID, outletID), db_vms);
                    err = incubeQuery.ExecuteNoneQuery(dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nRemoving customer outlet [" + customerCode + "] linking to groups failed");
                        return string.Empty;
                    }

                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("CustomerID", customerID);
                    QueryBuilderObject.SetField("OutletID", outletID);
                    QueryBuilderObject.SetField("GroupID", customerGroupID);
                    err = QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        WriteMessage("\r\nLinking customer outlet [" + customerCode + "] linking to groups failed");
                        return string.Empty;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nAdding customer outlet [" + customerCode + "] info failed\r\n");
                return string.Empty;
            }
            return customerID;
        }
        public override void UpdateCustomer()
        {
            try
            {
                #region(Member Data)

                string Query;
                DataTable dtCustomers;
                string barCode, customerDescAr, customerDescEn, phoneNo, fax, email, addressAr, addressEn, taxable, custGroup, custGroupDesc;
                string creditLimit, paymentTerm, onHold, gpsLong, gpsLat, custOrg;
                string outletCode = string.Empty;
                string customerCode;
                string custAccountID = string.Empty, outAccountID = string.Empty;
                string balance, CommitmentLimit;
                string masterCustomerID;
                string paymentTermID = string.Empty, customerGroupID, customerID, outletID = string.Empty, customerTypeID;
                bool newOutlet = false;
                bool isMaster = false;
                string country, state, city, area, street, streetID, orgID;

                #endregion

                WriteMessage("\r\nUpdating Customers ..Start\r\n");
               

                #region(Get All Customers)
                //#if Production
                try
                {
                    MCustomer mCustomer = new MCustomer();
                   
                    dtCustomers = ProxyService.HH_Get_POS_Info(mCompany, mCustomer, out errorMessage);
                    if (errorMessage != string.Empty)
                    {
                        throw (new Exception(errorMessage));
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    WriteMessage("Error in retrieving customers data\r\n");
                    return;
                }
                //#else
                //                Query = "SELECT * FROM SAPCustomer";
                //                dtCustomers = new DataTable();
                //                incubeQuery = new InCubeQuery(Query, db_vms);
                //                if (incubeQuery.Execute() != InCubeErrors.Success)
                //                {
                //                    WriteMessage("Error in retrieving customers data\r\n");
                //                    return;
                //                }
                //                dtCustomers = incubeQuery.GetDataTable();
                //#endif

                if (dtCustomers == null || dtCustomers.Rows.Count == 0)
                {
                    WriteMessage("No customers found ..\r\n");
                    return;
                }


                #endregion


                ClearProgress();
                SetProgressMax(dtCustomers.Rows.Count);
                TotalUpdated = 0;
                TotalInserted = 0;
                TotalIgnored = 0;

                for (int i = 0; i < dtCustomers.Rows.Count; i++)
                {
                    try
                    {
                        outletCode = dtCustomers.Rows[i]["CustomerCode"].ToString();
                         
                        ReportProgress("Updating customer");
                       

                        #region (Get Customer Info)

                        barCode = outletCode;
                        customerDescAr = dtCustomers.Rows[i]["CustomerName"].ToString();
                        customerDescEn = dtCustomers.Rows[i]["CustomerName"].ToString();
                        phoneNo = dtCustomers.Rows[i]["ClientPhone1"].ToString().Trim();
                        fax = dtCustomers.Rows[i]["ClientPhone2"].ToString();
                        email = string.Empty;
                        taxable = "0";
                        custGroup = dtCustomers.Rows[i]["GroupCode"].ToString();
                        custGroupDesc = dtCustomers.Rows[i]["GroupDescription"].ToString();
                        creditLimit = dtCustomers.Rows[i]["CreditLimit"].ToString();
                        paymentTerm = dtCustomers.Rows[i]["PaymentTerm"].ToString();
                        balance = dtCustomers.Rows[i]["balance"].ToString();
                        onHold = dtCustomers.Rows[i]["onHold"].ToString().Trim() == "Y" ? "0" : "1";
                        taxable = dtCustomers.Rows[i]["taxable"].ToString().Trim() == "Y" ? "1" : "0";
                        gpsLong = "0";
                        gpsLat = "0";

                        country = dtCustomers.Rows[i]["ClientCountry"].ToString();
                        state = dtCustomers.Rows[i]["ClientCountry"].ToString();
                        city = dtCustomers.Rows[i]["ClientCounty"].ToString();
                        area = dtCustomers.Rows[i]["ClientCity"].ToString();
                        street = dtCustomers.Rows[i]["ClientAddress"].ToString();
                        addressAr = dtCustomers.Rows[i]["ClientAddress"].ToString();
                        addressEn = dtCustomers.Rows[i]["ClientAddress"].ToString();
                       // CommitmentLimit = dtCustomers.Rows[i]["ClientMaxChequeValue"].ToString();
                        customerCode = dtCustomers.Rows[i]["MasterCustomerCode"].ToString();
                        custOrg = "0";

                        isMaster = false;
                        if (customerCode == string.Empty)
                        {
                            customerCode = outletCode;
                            isMaster = true;
                        }

                        #endregion

                        dbTrans = new InCubeTransaction();
                        err = dbTrans.BeginTransaction(db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("Customer [" + customerCode + "] was not imported, Failed in opening DB trasnaction\r\n");
                            throw (new Exception(""));
                        }

                        int payTermDays = 0;
                        if (int.TryParse(paymentTerm, out payTermDays) && payTermDays > 0)
                        {
                            customerTypeID = "2";
                            paymentTermID = InsertUpdatePaymentTerm(paymentTerm);
                            if (paymentTermID == string.Empty)
                            {
                                throw (new Exception(""));
                            }
                        }
                        else
                        {
                            customerTypeID = "1";
                            paymentTermID = "0";
                            creditLimit = "0";
                        }

                        orgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + custOrg + "'", db_vms, dbTrans);
                        if (orgID == string.Empty)
                        {
                            WriteMessage("\r\nOrganization [" + custOrg + "] doesn't exist");
                            throw (new Exception(""));
                        }

                        streetID = InsertUpdateGeographicalLocations(country, state, city, area, street);
                        if (streetID == string.Empty)
                        {
                            throw (new Exception(""));
                        }

                        masterCustomerID = InsertUpdateMasterCustomer(customerCode, phoneNo, fax, email, customerDescEn, addressEn, customerDescAr, streetID, addressAr, balance, creditLimit, onHold, orgID, isMaster, "0", ref custAccountID);
                        if (masterCustomerID == string.Empty)
                        {
                            throw (new Exception(""));
                        }

                        customerGroupID = InsertUpdateCustomerGroup(custGroup, custGroup, custGroup);
                        if (customerGroupID == string.Empty)
                        {
                            throw (new Exception(""));
                        }

                        customerID = InsertUpdateCustomerOutlet(outletCode, masterCustomerID, phoneNo, fax, email, customerTypeID, "0", taxable, barCode, paymentTermID, customerDescEn, customerDescAr, addressEn, addressAr, streetID, gpsLong, gpsLat, true, creditLimit, balance, custAccountID, orgID, customerGroupID, "0", ref newOutlet, ref outletID, ref outAccountID);
                        if (customerID == string.Empty)
                        {
                            throw (new Exception(""));
                        }

                        err = dbTrans.Commit();
                        if (err != InCubeErrors.Success)
                        {
                            WriteMessage("Error in committing db transaction\r\n");
                            throw (new Exception(""));
                        }

                        if (newOutlet)
                            TotalInserted++;
                        else
                            TotalUpdated++;

                        if (customerID != masterCustomerID)
                        {
                            MoveCustomer(customerID, outletID, masterCustomerID, outletCode, phoneNo, fax, email, customerTypeID, onHold, taxable, barCode, paymentTermID, streetID, gpsLong, gpsLat, orgID);

                            if (err != InCubeErrors.Success)
                            {
                                dbTrans.Rollback();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TotalIgnored++;
                        if (ex.StackTrace != string.Empty)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                            WriteMessage("\r\nUnexpected error happened, failed to update customer [" + outletCode + "+]\r\n");
                        }
                        if (dbTrans != null)
                        {
                            dbTrans.Rollback();
                        }
                    }
                }

                WriteMessage("\r\nCustomers importing completed.\r\n");
                WriteMessage("Total inserted customers: " + TotalInserted + "\r\n");
                WriteMessage("Total updated customers: " + TotalUpdated + "\r\n");
                WriteMessage("Total ignored customers: " + TotalIgnored + "\r\n");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nUnexpected error happened, failed to update customers\r\n");
            }
        }
        private void MoveCustomer(string customerID, string outletID, string masterCustomerID, string customerCode, string phoneNo, string faxNo, string email, string customerTypeID, string onHold, string taxable, string barCode, string paymentTermID, string streetID, string gpsLongitude, string gpsLatitude, string orgID)
        {
            try
            {
                #region(Member Data)

                string Query;
                string newOutletID;
                DataRow[] dr;
                object count = null;

                #endregion

                #region (Get Customer Tables)

                if (dtCustomerTables == null)
                {
                    Query = @"SELECT TBL.*, CASE WHEN FK.name IS NULL THEN 0 ELSE 1 END HasFK FROM
                             (SELECT DISTINCT
                             TableName = t.NAME,
                             RowCounts = p.rows,
	                         CASE WHEN tout.name IS NULL THEN 0 ELSE 1 END HasOutletID,
	                         tcust.colCustomer,
	                         ISNULL(tout.colOutlet,'') colOutlet
                             FROM 
                             sys.tables t
                             INNER JOIN      
                             sys.indexes i ON t.OBJECT_ID = i.object_id
                             INNER JOIN 
                             sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                             INNER JOIN
	                         sys.columns c on t.object_id = c.object_id
                             LEFT OUTER JOIN
	                         (select t.name,c.name colCustomer from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name like '%CustomerID%') tcust ON tcust.name = t.name
                             LEFT OUTER JOIN
	                         (select t.name,c.name colOutlet from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name like '%outletID%') tout ON tout.name = t.name
                             WHERE 
                             t.is_ms_shipped = 0 AND tcust.name IS NOT NULL) TBL
                             LEFT OUTER JOIN
	                         (select tbl.name
	                         from sys.foreign_key_columns fkCol
	                         inner join sys.foreign_keys fk on fkCol.constraint_object_id = fk.object_id
	                         INNER JOIN sys.columns col on fkCol.parent_object_id = col.object_id and fkCol.parent_column_id = col.column_id
	                         INNER JOIN sys.tables tbl on col.object_id = tbl.object_id
	                         where col.name LIKE '%CustomerID%' or col.name LIKE '%OutletID%'
	                         group by tbl.name
	                         having count(*) = 2) FK ON TBL.TableName = FK.name
	                         where tbl.RowCounts > 0
                             ORDER BY HasFK DESC";

                    dtCustomerTables = new DataTable();
                    incubeQuery = new InCubeQuery(Query, db_vms);
                    if (incubeQuery.Execute() == InCubeErrors.Success)
                    {
                        dtCustomerTables = incubeQuery.GetDataTable();
                    }
                }

                #endregion

                dbTrans = new InCubeTransaction();
                err = dbTrans.BeginTransaction(db_vms);
                if (err != InCubeErrors.Success)
                    return;

                #region(Create New Outlet)

                newOutletID = string.Empty;
                newOutletID = GetFieldValue("CustomerOutlet", "ISNULL(MAX(OutletID),0)+1", "CustomerID = " + masterCustomerID, db_vms, dbTrans);
                QueryBuilderObject = new QueryBuilder();
                QueryBuilderObject.SetStringField("CustomerID", masterCustomerID);
                QueryBuilderObject.SetStringField("OutletID", newOutletID);
                QueryBuilderObject.SetStringField("CustomerCode", customerCode);
                QueryBuilderObject.SetStringField("Barcode", barCode);
                QueryBuilderObject.SetStringField("Phone", phoneNo);
                QueryBuilderObject.SetStringField("Fax", faxNo);
                QueryBuilderObject.SetStringField("Email", email);
                QueryBuilderObject.SetStringField("StreetID", streetID);
                QueryBuilderObject.SetStringField("GPSLatitude", gpsLongitude);
                QueryBuilderObject.SetStringField("GPSLongitude", gpsLatitude);
                QueryBuilderObject.SetStringField("Taxeable", taxable);
                QueryBuilderObject.SetStringField("CustomerTypeID", customerTypeID);
                QueryBuilderObject.SetStringField("CurrencyID", "1");
                QueryBuilderObject.SetStringField("OnHold", onHold);
                QueryBuilderObject.SetStringField("StreetAddress", "0");
                QueryBuilderObject.SetStringField("InActive", "0");
                QueryBuilderObject.SetStringField("Notes", "0");
                QueryBuilderObject.SetStringField("SkipCreditCheck", "0");
                QueryBuilderObject.SetStringField("PaymentTermID", paymentTermID);
                QueryBuilderObject.SetStringField("OrganizationID", orgID);
                QueryBuilderObject.SetStringField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("CreatedDate", DateTime.Now);
                QueryBuilderObject.SetStringField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetDateField("UpdatedDate", DateTime.Now);
                err = QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms, dbTrans);
                if (err != InCubeErrors.Success)
                    return;

                #endregion

                #region(Update Records)

                dr = dtCustomerTables.Select("HasOutletID = 1 AND TableName <> 'CustomerOutlet'");
                if (dr.Length > 0)
                {
                    for (int i = 0; i < dr.Length; i++)
                    {
                        if ((dr[i]["colCustomer"].ToString() == "TargetCustomerID" && dr[i]["colOutlet"].ToString() == "UsedForOutletID")
                            || (dr[i]["colCustomer"].ToString() == "UsedForCustomerID" && dr[i]["colOutlet"].ToString() == "TargetOutletID"))
                            continue;

                        Query = string.Format("UPDATE [{0}] SET {1} = {2}, {3} = {4} WHERE {1} = {5} AND {3} = {6}", dr[i]["TableName"].ToString(), dr[i]["colCustomer"].ToString(), masterCustomerID, dr[i]["colOutlet"].ToString(), newOutletID, customerID, outletID);
                        incubeQuery = new InCubeQuery(Query, db_vms);
                        err = incubeQuery.ExecuteNoneQuery(dbTrans);
                        if (err != InCubeErrors.Success)
                            return;
                    }
                }

                #endregion

                #region(Delete Records)

                Query = string.Format("DELETE FROM CustomerOutlet WHERE CustomerID = {0} AND OutletID = {1}", customerID, outletID);
                incubeQuery = new InCubeQuery(Query, db_vms);
                err = incubeQuery.ExecuteNoneQuery(dbTrans);
                if (err != InCubeErrors.Success)
                    return;

                incubeQuery = new InCubeQuery(db_vms, "SELECT COUNT(*) FROM CustomerOutlet WHERE CustomerID = " + customerID);
                if (incubeQuery.ExcuteScaler(dbTrans, ref count) != InCubeErrors.Success || count == null || string.IsNullOrEmpty(count.ToString()))
                    return;

                if (int.Parse(count.ToString()) == 0)
                {
                    dr = dtCustomerTables.Select("HasOutletID = 0 AND TableName <> 'Customer'");
                    if (dr.Length > 0)
                    {
                        for (int i = 0; i < dr.Length; i++)
                        {
                            Query = string.Format("DELETE FROM [{0}] WHERE {1} = {2}", dr[i]["TableName"].ToString(), dr[i]["colCustomer"].ToString(), customerID);
                            incubeQuery = new InCubeQuery(Query, db_vms);
                            err = incubeQuery.ExecuteNoneQuery(dbTrans);
                            if (err != InCubeErrors.Success)
                                return;
                        }
                    }

                    Query = string.Format("DELETE FROM Customer WHERE CustomerID = {0}", customerID);
                    incubeQuery = new InCubeQuery(Query, db_vms);
                    err = incubeQuery.ExecuteNoneQuery(dbTrans);
                    if (err != InCubeErrors.Success)
                        return;
                }
                #endregion

                err = dbTrans.Commit();
            }
            catch (Exception ex)
            {
                err = InCubeErrors.Error;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("\r\nUnexpected error happened, failed to update customers\r\n");
            }
        }

        #endregion

        public void CheckSAPData(string item)
        {
            try
            {
                WriteMessage("\r\nImporting " + item + " ..\r\n");

                DataTable dtData = new DataTable();
                string colQuery, valuesQuery;
                StringBuilder allInserts = new StringBuilder();

                switch (item)
                {
                    case "Item":
                        dtData = ProxyService.HH_Get_Item(mCompany, out errorMessage);
                        break;
                    case "Customer":
                        MCustomer mCustomer = new MCustomer();
                        dtData = ProxyService.HH_Get_POS_Info(mCompany, mCustomer, out errorMessage);
                        break;
                    case "Salesperson":
                        dtData = ProxyService.HH_Get_User(mCompany, out errorMessage);
                        break;
                    case "Vehicles":
                        MWareHouse mWareHouse = new MWareHouse();
                        dtData = ProxyService.HH_Get_Stores(mCompany, mWareHouse, out errorMessage);
                        break;
                    case "Price":
                        dtData = ProxyService.HH_Get_PriceList_H(mCompany, out errorMessage);
                        break;
                    case "PriceDefinition":
                        dtData = ProxyService.HH_Get_PriceList_D(mCompany, out errorMessage);
                        break;
                    case "Bank":
                        dtData = ProxyService.HH_Get_Banks(mCompany, out errorMessage);
                        break;
                }

                //frmView frm = new frmView(item, dtData);
                //frm.Show();

                string tableName = "SAP" + item;
                StringBuilder columns = new StringBuilder();
                for (int i = 0; i < dtData.Columns.Count; i++)
                {
                    columns.Append(dtData.Columns[i].ColumnName + " ");
                    switch (dtData.Columns[i].DataType.ToString())
                    {
                        case "System.Int16":
                            columns.Append("INT NULL");
                            break;
                        case "System.Decimal":
                            columns.Append("numeric(19,9) NULL");
                            break;
                        default:
                            columns.Append("NVARCHAR(200) NULL");
                            break;
                    }
                    if (i != dtData.Columns.Count - 1)
                    {
                        columns.AppendLine(",");
                    }
                }

                string query = string.Format(@"IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'{0}') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
                BEGIN
                DROP TABLE {0}
                END
                
                CREATE TABLE {0}(
                        {1}
                 );
                ", tableName, columns.ToString());

                incubeQuery = new InCubeQuery(query, db_vms);
                err = incubeQuery.ExecuteNonQuery();

                if (err != InCubeErrors.Success)
                {
                    WriteMessage("Error in dropping/creating table [" + tableName + "]\r\n");
                    return;
                }

                allInserts.AppendLine(query);

                ClearProgress();
                SetProgressMax(dtData.Rows.Count);
                TotalIgnored = 0;
                TotalInserted = 0;
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    ReportProgress("");

                          colQuery = "INSERT INTO " + tableName + " (";
                    valuesQuery = ") VALUES (";
                    for (int j = 0; j < dtData.Columns.Count; j++)
                    {
                        colQuery += dtData.Columns[j].ColumnName + ",";
                        valuesQuery += "'" + dtData.Rows[i][j].ToString().Replace("'", "''") + "',";
                    }
                    query = colQuery.TrimEnd(new char[] { ',' }) + valuesQuery.TrimEnd(new char[] { ',' }) + ");";
                    incubeQuery = new InCubeQuery(db_vms, query);
                    err = incubeQuery.ExecuteNonQuery();
                    if (err != InCubeErrors.Success)
                    {
                        TotalIgnored++;
                        WriteMessage("Error in inserting row " + i + 1 + "\r\n");
                    }
                    else
                    {
                        allInserts.AppendLine(query);
                        TotalInserted++;
                    }
                }

                WriteImportSAPScript(allInserts.ToString(), item);
                WriteMessage("Importing completed: " + TotalInserted + " rows were inserted, " + TotalIgnored + " rows were ignored\r\n");

                if (item == "Price")
                {
                    CheckSAPData("PriceDefinition");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                WriteMessage("Importing failed\r\n");
            }
        }
    }
}
