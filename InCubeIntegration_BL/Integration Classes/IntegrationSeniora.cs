using System; 
using System.Text;
using System.Data;
using System.Data.Odbc;
using InCubeLibrary; 
using System.Xml;
using System.Globalization;
using System.IO; 
using System.Collections;
using InCubeIntegration_DAL;
using System.ComponentModel;

namespace InCubeIntegration_BL
{
   
    class IntegrationSeniora : IntegrationBase
    {

        #region Definitions

        QueryBuilder QueryBuilderObject = new QueryBuilder();
        protected InCubeDatabase db_ERP;

        InCubeErrors err;
        private long UserID;
        int TotalInserted;
        int TotalUpdated;
        int TotalIgnored;
        InCubeQuery incubeQuery ;
        InCubeTransaction dbTrans = null ;
        string DateFormat = "dd/MMM/yyyy";
        string OrganizationID = "";
        CultureInfo EsES = new CultureInfo("es-ES");
        OdbcConnection Conn;
        string ConnectionString = "";
        ArrayList arrPromotedItems = new ArrayList();
        string CurrentDirectory = "";
        int TotalRows = 0; int Inserted = 0; int Updated = 0; int Skipped = 0;
        string StagingTable = "";
        string _WarehouseID = "-1";
        BackgroundWorker bgwCheckProgress;
        string FilePathSalesAndReturn = "", FilePathSalesAndReturnWithdrwal = "",
               FilePathSalesOrder="", FilePathPayments="", FilePathTransfer="",
           FilePathSalesOrderWithdrwal="";
        internal IntegrationSeniora(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {

            string _dataSourceFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\DataSources.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_dataSourceFilePath);
            ConnectionString = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ERP']/Data").InnerText;
            // Conn = new OdbcConnection(ConnectionString);
             
            db_ERP = new InCubeDatabase();
            InCubeErrors err = db_ERP.Open("ERP", "IntegrationSeniora");
            if (err != InCubeErrors.Success)
            {
                WriteMessage("Unable to connect to Intermediate database");
                return;
            }

            FilePathSalesAndReturn = xmlDoc.SelectSingleNode("Connections/FilePathSalesAndReturn").InnerText;
            FilePathSalesAndReturnWithdrwal = xmlDoc.SelectSingleNode("Connections/FilePathSalesAndReturnWithdrwal").InnerText;
            FilePathSalesOrder = xmlDoc.SelectSingleNode("Connections/FilePathSalesOrder").InnerText;
            FilePathSalesOrderWithdrwal = xmlDoc.SelectSingleNode("Connections/FilePathSalesOrderWithdrwal").InnerText;
            FilePathPayments = xmlDoc.SelectSingleNode("Connections/FilePathPayments").InnerText;
            FilePathTransfer = xmlDoc.SelectSingleNode("Connections/FilePathTransfer").InnerText;


            
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
                    GetExecutionResults(StagingTable, ref TotalRows, ref Inserted, ref Updated, ref Skipped, db_vms);
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
        #endregion

        #region Update Data

        #region UpdateItem

        public override void UpdateItem()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                InCubeErrors err;
                object field = new object();

                string SelectItems = @"SELECT  [ItemCode]
      ,[UOMsales]
    ,[ItemDescriptionAR]
      ,[ItemDescriptionEN]
      ,[OrganizationCode]
      ,[ItemCategoryCode]
      ,[Categorydescription]
      ,[Origin]
      ,[StockUnit]
      ,[NewCode]
  FROM  [invan_Material]";

                DefaultOrganization();

             

                InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
                ItemQuery.Execute();

                DataTable DT = new DataTable();

                DT = ItemQuery.GetDataTable();

                ItemQuery.Close();
                ClearProgress();
                SetProgressMax(DT.Rows.Count);
               
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    ReportProgress("Updating Items");
                  
                    string ItemCode = DT.Rows[i][9].ToString();

                    string ItemDesE = DT.Rows[i][3].ToString();

                    string ItemDesA = DT.Rows[i][2].ToString();

                    string DivisionCode = "01";

                    string DivisionDesE = "Siniora";

                    string DivisionDesA = "Siniora";

                    string CategoryCode = DT.Rows[i][5].ToString();

                    string CategoryDesE = DT.Rows[i][6].ToString();

                    string CategoryDesA = DT.Rows[i][6].ToString();

                    string PackDescE = DT.Rows[i][8].ToString().Trim();

                    string PackDescA = DT.Rows[i][8].ToString().Trim();
                    string ScalaCode = DT.Rows[i][0].ToString();
                    string Oragen = DT.Rows[i][7].ToString();

                    string ConvFactor = "1";// DT.Rows[i][6].ToString();

                    string OrgCode = DT.Rows[i][4].ToString();
                    string OrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + OrgCode + "'", db_vms);
                    if (OrgID == "") OrgID = "1";
                   
                    #region ItemDivision

                    string DivisionID = GetFieldValue("Division", "DivisionID", "DivisionCode = '" + DivisionCode + "'", db_vms);

                    if (DivisionID == string.Empty)
                    {
                        DivisionID = GetFieldValue("Division", "isnull(MAX(DivisionID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("DivisionCode", "'" + DivisionCode + "'");
                        QueryBuilderObject.SetField("OrganizationID", OrgID);
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.InsertQueryString("Division", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + DivisionDesE + "'");
                        QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);

                        QueryBuilderObject.SetField("DivisionID", DivisionID);  // Arabic Description same to English
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + DivisionDesA + "'");
                        QueryBuilderObject.InsertQueryString("DivisionLanguage", db_vms);
                    }
                    else
                    {

                        QueryBuilderObject.SetField("OrganizationID", OrgID);
                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        QueryBuilderObject.UpdateQueryString("Division", "DivisionID=" + DivisionID, db_vms);

                    }

                    #endregion

                    #region ItemCategory

                    string ItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "' and DivisionID=" + DivisionID, db_vms);

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
                        QueryBuilderObject.SetField("Description", "'" + CategoryDesE + "'");
                        QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID);  // Arabic Description
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "'" + CategoryDesA + "'");
                        QueryBuilderObject.InsertQueryString("ItemCategoryLanguage", db_vms);

                    }
                    else
                    {
                        string _existItemCategoryID = GetFieldValue("ItemCategory", "ItemCategoryID", "ItemCategoryCode = '" + CategoryCode + "' AND DivisionID =" + DivisionID.ToString(), db_vms);
                        if (ItemCategoryID == string.Empty)
                        {
                           WriteMessage("\r\n");
                            WriteMessage(" Item Category " + CategoryDesE + " is defined twice , the duplicated division : " + DivisionDesE);
                        }
                    }

                    #endregion

                    #region PackType

                    string PacktypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " Description = '" + PackDescE + "' AND LanguageID = 1", db_vms);

                    if (PacktypeID == string.Empty)
                    {
                        PacktypeID = GetFieldValue("PackType", "isnull(MAX(PackTypeID),0) + 1", db_vms);

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.InsertQueryString("PackType", db_vms);

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + PackDescE + "'");
                        QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("LanguageID", "2");
                        QueryBuilderObject.SetField("Description", "N'" + PackDescA + "'");
                        QueryBuilderObject.InsertQueryString("PackTypeLanguage", db_vms);

                    }

                    #endregion

                    string ItemID = "";

                    ItemID = GetFieldValue("Item", "ItemID", "PackDefinition='" + ScalaCode + "'", db_vms);
                    if (ItemID == string.Empty)
                    {
                        ItemID = GetFieldValue("Item", "isnull(MAX(ItemID),0) + 1", db_vms);
                    }

                    #region Item

                    string ExistItem = GetFieldValue("Item", "ItemID", "ItemID = " + ItemID, db_vms);
                    if (ExistItem != string.Empty) // Exist Item --- Update Query
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("ItemCode", "'" + ItemCode + "'");
                        QueryBuilderObject.SetField("ItemCategoryID", ItemCategoryID.ToString());

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
                        QueryBuilderObject.SetField("Inactive", "0");
                        QueryBuilderObject.SetField("PackDefinition", "'" + ScalaCode + "'");
                        QueryBuilderObject.SetField("Origin", "'" + Oragen + "'");
                        
                       
                        QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("ItemType", "1");
                        QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                        QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                        QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                        err = QueryBuilderObject.InsertQueryString("Item", db_vms);
                    }

                    #endregion

                    #region ItemLanguage

                    ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 1", db_vms);
                    if (ExistItem != string.Empty)
                    {
                        QueryBuilderObject.SetField("Description", "'" + ItemDesE + "'");
                        QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 1", db_vms);

                    }
                    else
                    {
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("LanguageID", "1");
                        QueryBuilderObject.SetField("Description", "'" + ItemDesE + "'");
                        QueryBuilderObject.InsertQueryString("ItemLanguage", db_vms);
                    }

                    if (ItemDesA != string.Empty)
                    {
                        ExistItem = GetFieldValue("ItemLanguage", "ItemID", "ItemID = " + ItemID + " AND LanguageID = 2", db_vms); // ARABIC
                        if (ExistItem != string.Empty)
                        {
                            QueryBuilderObject.SetField("Description", "N'" + ItemDesA + "'");
                            QueryBuilderObject.UpdateQueryString("ItemLanguage", " ItemID =" + ItemID + " AND LanguageID = 2", db_vms);
                        }
                        else
                        {
                            QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + ItemDesA + "'");
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

                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.UpdateQueryString("Pack", "PackID = " + PackID, db_vms);
                    }
                    else
                    {
                        PackID = int.Parse(GetFieldValue("Pack", "ISNULL(MAX(PackID),0) + 1", db_vms));

                        QueryBuilderObject.SetField("PackID", PackID.ToString());
                        QueryBuilderObject.SetField("ItemID", ItemID.ToString());
                        QueryBuilderObject.SetField("PackTypeID", PacktypeID);
                        QueryBuilderObject.SetField("Quantity", ConvFactor);
                        QueryBuilderObject.SetField("EquivalencyFactor", "0");
                        QueryBuilderObject.InsertQueryString("Pack", db_vms);
                    }

                    #endregion
                }

                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< ITEMS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                //Conn.Close();
            }
        }

        #endregion

        #region UpdateCustomer
        //internal override void UpdateNewCustomer(string CustomerNo)
        //{
        //    UpdateCustomer("VScalaNewCustomers01", CustomerNo);
        //}
        //internal override void UpdateAllCustomer(string CustomerNo)
        //{
        //    UpdateCustomer("VScalaAllCustomers01", CustomerNo);
        //}
       public override void UpdateCustomer()//string TableName, string CustomerNo)
        {
            int CCCC = 0;
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;

                int Activecust = 0;
                int TaxableCust = 1;
                InCubeErrors err;
                object field = new object();

                string SelectItems = @"SELECT   [CUSTCODE]
      ,[CUSTNAME]
      ,[CUSTBARCODE]
      ,[PHONE]
      ,[FAx]
      ,[GroupCode]
      ,[GroupNAME]
      ,[PYAMENTERM]
      ,[iscredit]
      ,[creditlimt]
      ,[balance]
      ,[CURRENCY_CODE]
      ,[PayTerm]
      ,[Active]
      ,[Price List Code],
	  p.Credit_Days
	  ,p.PaymentDesc
  FROM [invan_customers] c inner join invan_payment_term p
  on c.PYAMENTERM=p.Payment_Term_ID";

                
                DefaultOrganization();



                InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
                ItemQuery.Execute();

                DataTable DT = new DataTable();

                DT = ItemQuery.GetDataTable();

                ItemQuery.Close();
                ClearProgress();
                SetProgressMax(DT.Rows.Count); 

                for (int i = 0; i < DT.Rows.Count; i++)
                {


                    ReportProgress("Updating customers");

                 
                    string CustomerCode = DT.Rows[i][0].ToString();

                    string CustomerDesA = DT.Rows[i][1].ToString();

                    string CustomerDesE = DT.Rows[i][1].ToString();

                    string Phone = DT.Rows[i][3].ToString();

                    string Fax = DT.Rows[i][4].ToString();

                    string GroupCode = DT.Rows[i][5].ToString();
                    string Group = DT.Rows[i][6].ToString()  ;
                    string Taxable = "1";

                    string CreditLimit = DT.Rows[i][9].ToString();

                    string Balance = DT.Rows[i][10].ToString();

                    string PaymentsTerm = DT.Rows[i][15].ToString();
                    string PaymentsTermDesc = DT.Rows[i][7].ToString() + "-" + DT.Rows[i][12].ToString();

                    string Active = DT.Rows[i][13].ToString();

                    string MasterCustomerCode ="";

                    string PriceGroupCode = "P"+DT.Rows[i][14].ToString();
                    string PriceGroup = "Price Group "+DT.Rows[i][14].ToString();
                    string OrgID = "1";// GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + OrgCode + "'", db_vms);
                   // if (OrgID == "") OrgID = "1";

                    if (CustomerCode == string.Empty)
                        continue;

                    string CustomerID = "0";

                    string ExistCustomer = "";

                    string IsCredit = DT.Rows[i][8].ToString();
                    

 

                    if (MasterCustomerCode.Trim() == string.Empty) // Master Customer
                    {
                        CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode = '" + CustomerCode + "'", db_vms);
                        if (CustomerID == string.Empty)
                        {
                            CustomerID = GetFieldValue("Customer", "isnull(MAX(CustomerID),0) + 1", db_vms);
                        }

                        #region Customer
                        ExistCustomer = GetFieldValue("Customer", "CustomerID", "CustomerID = " + CustomerID, db_vms);
                        if (ExistCustomer != string.Empty) // Exist Customer --- Update Query
                        {
                            TOTALUPDATED++;

                            QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                            QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                            QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                            QueryBuilderObject.SetField("InActive", Active.Trim()=="1" ?"0":"1");
                           // QueryBuilderObject.SetField("StreetID", CityID.ToString());
                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.UpdateQueryString("Customer", " CustomerID = " + CustomerID, db_vms);


                        }
                        else // New Customer --- Insert Query
                        {
                            TOTALINSERTED++;

                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                            QueryBuilderObject.SetField("Fax", "'" + Fax + "'");
                            //QueryBuilderObject.SetField("Email", "'" + Email + "'");
                            QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                            QueryBuilderObject.SetField("OnHold", Activecust.ToString());
                         //   QueryBuilderObject.SetField("StreetID", CityID.ToString());
                            QueryBuilderObject.SetField("StreetAddress", "0");
                            QueryBuilderObject.SetField("InActive", Active.Trim() == "1" ? "0" : "1");
                            QueryBuilderObject.SetField("New", "0");

                            QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                            QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                            QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            err = QueryBuilderObject.InsertQueryString("Customer", db_vms);
                            //MessageBox.Show(err.ToString());
                        }

                        #endregion

                        #region CustomerLanguage
                        ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                        if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                        {
                            QueryBuilderObject.SetField("Description", "'" + CustomerDesE + "'");
                         //   QueryBuilderObject.SetField("Address", "'" + AddressE + "'");
                            QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 1", db_vms);
                        }
                        else  // New CustomerLanguage --- Insert Query
                        {
                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + CustomerDesE + "'");
                           // QueryBuilderObject.SetField("Address", "'" + AddressE + "'");
                            QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                        }

                        ExistCustomer = GetFieldValue("CustomerLanguage", "CustomerID", "CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms); // ARABIC
                        if (ExistCustomer != string.Empty) // Exist CustomerLanguage --- Update Query
                        {
                            QueryBuilderObject.SetField("Description", "N'" + CustomerDesA + "'");
                          //  QueryBuilderObject.SetField("Address", "N'" + AddressA + "'");
                            QueryBuilderObject.UpdateQueryString("CustomerLanguage", "  CustomerID = " + CustomerID + " AND LanguageID = 2", db_vms);
                        }
                        else  // New CustomerLanguage --- Insert Query
                        {
                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + CustomerDesA + "'");
                           // QueryBuilderObject.SetField("Address", "N'" + AddressA + "'");
                            QueryBuilderObject.InsertQueryString("CustomerLanguage", db_vms);
                        }

                        #endregion

                        int AccountID = 1;

                        ExistCustomer = GetFieldValue("AccountCust INNER JOIN Account on AccountCust.AccountID=Account.AccountID", "Account.AccountID", "CustomerID = " + CustomerID + " and OrganizationID = " + OrgID, db_vms);
                        if (ExistCustomer != string.Empty)
                        {
                            AccountID = int.Parse(GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms));

                            QueryBuilderObject.SetField("OrganizationID", OrgID);
                            QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                            QueryBuilderObject.SetField("Balance", Balance);
                            QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + AccountID.ToString(), db_vms);

                        }
                        else
                        {
                            AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                            QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                            QueryBuilderObject.SetField("AccountTypeID", "1");//customer account
                            QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                            QueryBuilderObject.SetField("Balance", Balance);
                            QueryBuilderObject.SetField("GL", "0");
                            QueryBuilderObject.SetField("OrganizationID", OrgID);
                            QueryBuilderObject.SetField("CurrencyID", "1");
                            QueryBuilderObject.InsertQueryString("Account", db_vms);

                            QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                            QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                            QueryBuilderObject.InsertQueryString("AccountCust", db_vms);

                            QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "1");
                            QueryBuilderObject.SetField("Description", "'" + CustomerDesE.Trim() + " Account'");
                            QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                            QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                            QueryBuilderObject.SetField("LanguageID", "2");
                            QueryBuilderObject.SetField("Description", "N'" + CustomerDesA.Trim() + " Account'");
                            QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
                        }

                        CreateCustomerOutlet(CustomerCode, GroupCode, Group, PriceGroupCode, PriceGroup, IsCredit, PaymentsTerm, PaymentsTermDesc, CustomerDesE, "", CustomerDesA, "", Phone, Fax, "0", Taxable, CustomerCode, CreditLimit, Balance, CustomerCode, "", "", OrgID, Active.Trim() == "1" ? "0" : "1");

                    }
                    else // Outlet
                    {
                        CreateCustomerOutlet(CustomerCode, GroupCode, Group, PriceGroupCode, PriceGroup, IsCredit, PaymentsTerm, PaymentsTermDesc, CustomerDesE, "", CustomerDesA, "", Phone, Fax, "0", Taxable, MasterCustomerCode, CreditLimit, Balance, CustomerCode, "", "", OrgID, Active.Trim() == "1" ? "0" : "1");
                    }

                }//while success

               //Delete customer from Cash customer , credit customer
                 InCubeQuery DeleteCustomerGroupCash = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where groupid = (select Groupid from customergroup where groupcode = 'Cash-c')");
                 DeleteCustomerGroupCash.ExecuteNonQuery();
                 InCubeQuery DeleteCustomerGroupCredit = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where groupid = (select Groupid from customergroup where groupcode = 'Credit-c')");
                 DeleteCustomerGroupCredit.ExecuteNonQuery();

                //Insert into CustomerOutletGroup Cash and Credit and DisCustomer
                 InCubeQuery InsertCustomerGroupCash = new InCubeQuery(db_vms, @"Insert into CustomerOutletGroup select customerid , outletid , (select Groupid from customergroup where groupcode = 'Cash-c') groupID
                 from customeroutlet where CustomerTypeid = 1 and convert(varchar,customerid)+'-'+convert(varchar,outletid) not in 
				 (select convert(varchar,customerid)+'-'+convert(varchar,outletid) from customeroutletgroup where groupid in (select groupid from CustomerGroup where groupcode = '99' or groupcode = 'Dis-C'))");
                 InsertCustomerGroupCash.ExecuteNonQuery();
                 InCubeQuery InsertCustomerGroupCredit = new InCubeQuery(db_vms, @"Insert into CustomerOutletGroup 
                 select customerid , outletid , (select Groupid from customergroup where groupcode = 'Credit-c') groupID
                 from customeroutlet where CustomerTypeid = 2 and convert(varchar,customerid)+'-'+convert(varchar,outletid) not in 
				 (select convert(varchar,customerid)+'-'+convert(varchar,outletid) from customeroutletgroup where groupid in (select groupid from CustomerGroup where groupcode = '99' or groupcode = 'Dis-C'))");
                 InsertCustomerGroupCredit.ExecuteNonQuery();
                 //Delete customer from DisCustomer
                 InCubeQuery DeleteCustomerGroupDisCustomer = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where groupid = (select Groupid from customergroup where groupcode = 'Dis-C')");
                 DeleteCustomerGroupDisCustomer.ExecuteNonQuery();
                 InCubeQuery InsertCustomerGroupDisCustomer = new InCubeQuery(db_vms, @"Insert into CustomerOutletGroup select customerid , outletid , (select Groupid from customergroup where groupcode = 'Dis-C') groupID
                 from customeroutlet where customercode like '21_%'");
                 InsertCustomerGroupDisCustomer.ExecuteNonQuery();
               //On hold DisCustomer 
                 InCubeQuery UpdateCustomerGroupDisCustomer = new InCubeQuery(db_vms, @"update  customeroutlet set OnHold = 1 where customercode like '21_%'");
                 UpdateCustomerGroupDisCustomer.ExecuteNonQuery();





                DT.Dispose();
               WriteMessage("\r\n");
                WriteMessage("<<< CUSTOMERS >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + " " + CCCC.ToString());
            }
            finally
            {
                //Conn.Close();
            }
        }

       private void CreateCustomerOutlet(string CustomerCode, string GroupCode, string GroupDesc, string PriceGroupCode, string PriceGroupDesc, string IsCreditCustomer, string Paymentterms, string Paymenttermsdesc, string CustomerDescriptionEnglish, string CustomerAddressEnglish, string CustomerDescriptionArabic, string CustomerAddressArabic, string Phonenumber, string Faxnumber, string OnHold, string Taxable, string HeadOfficeCode, string CreditLimit, string Balance, string CustomerBarCode, string email, string StreetID, string OrgID, string InActive)
        {
            string CustomerID;

            InCubeErrors err;
            object field = null;

            string ExistCustomer = "";
            string ExistCustomerAccount = "";

            CustomerID = GetFieldValue("Customer", "CustomerID", "CustomerCode='" + HeadOfficeCode + "'", db_vms);

            if (CustomerID.Equals(string.Empty))
            {
                return;
            }

            #region Get Customer Group
            string GroupID = "0";
            if (GroupCode.Trim().ToString() == "")
            {
                GroupID = "0";
            }
            else
            {
                GroupID = GetFieldValue("CustomerGroup", "GroupID", " GroupCode ='" + GroupCode.Trim().ToString() + "' ", db_vms);
            }

            if (GroupID == string.Empty && GroupCode.Trim().ToString() != "")
            {
                GroupID = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1 Where groupid<1000 ", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                QueryBuilderObject.SetField("GroupCode", "'" + GroupCode.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + GroupDesc.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + GroupDesc.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
            }


            string GroupID2 = "0";
            if (PriceGroupCode.Trim().ToString() == "")
            {
                GroupID2 = "0";
            }
            else
            {
                GroupID2 = GetFieldValue("CustomerGroup", "GroupID", " GroupCode ='" + PriceGroupCode.Trim().ToString() + "' ", db_vms);
            }

            if (GroupID2 == string.Empty && GroupCode.Trim().ToString() != "")
            {
                GroupID2 = GetFieldValue("CustomerGroup", "isnull(MAX(GroupID),0) + 1 Where groupid<1000 ", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                QueryBuilderObject.SetField("GroupCode", "'" + PriceGroupCode.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroup", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + PriceGroupDesc.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);

                QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + PriceGroupDesc.Trim().ToString() + "'");
                QueryBuilderObject.InsertQueryString("CustomerGroupLanguage", db_vms);
            }

            #endregion

            int Credit;
            string PaymentTermID = "1";

            if (IsCreditCustomer == "1")
            {
                Credit = 2;//at invan database 2 means credit customers

                PaymentTermID = GetFieldValue("PaymentTermLanguage", "PaymentTermID", "Description = '" + Paymenttermsdesc + "' and LanguageID=1", db_vms);
                if (PaymentTermID == string.Empty)
                {
                    PaymentTermID = GetFieldValue("PaymentTerm", "isnull(MAX(PaymentTermID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("PaymentTermTypeID", "1");
                    QueryBuilderObject.SetField("SimplePeriodWidth", Paymentterms);// number of days 45 for aljazeera
                    QueryBuilderObject.SetField("SimplePeriodID", "1"); //Days
                    QueryBuilderObject.InsertQueryString("PaymentTerm", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "1");
                    QueryBuilderObject.SetField("Description", "'" + Paymenttermsdesc + "'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);

                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                    QueryBuilderObject.SetField("LanguageID", "2");
                    QueryBuilderObject.SetField("Description", "'" + Paymenttermsdesc + "'");
                    QueryBuilderObject.InsertQueryString("PaymentTermLanguage", db_vms);
                }
            }
            else
            {
                Credit = 1; // cash customer
            }

            #region Customer Outlet and language

            string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerID = " + CustomerID + " AND CustomerCode = '" + CustomerCode + "'", db_vms);
            if (!OutletID.Trim().Equals(string.Empty))
            {
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("CustomerTypeID", Credit.ToString()); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("Taxeable", Taxable);
               // QueryBuilderObject.SetField("StreetID", StreetID);
                //QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                QueryBuilderObject.SetField("InActive", InActive);
                if (Credit == 2)
                {
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                }
                else
                {
                    QueryBuilderObject.SetField("PaymentTermID", "0");
                }
                err = QueryBuilderObject.UpdateQueryString("CustomerOutlet", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);

            }
            else
            {
                OutletID = GetFieldValue("CustomerOutlet", "isnull(MAX(OutletID),0) + 1", "CustomerID = " + CustomerID, db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("CustomerCode", "'" + CustomerCode + "'");
                //QueryBuilderObject.SetField("Barcode", "'" + CustomerBarCode + "'");
                QueryBuilderObject.SetField("Phone", "'" + Phonenumber + "'");
                QueryBuilderObject.SetField("Fax", "'" + Faxnumber + "'");
                QueryBuilderObject.SetField("Email", "'" + email + "'");
                QueryBuilderObject.SetField("Taxeable", Taxable);
                //QueryBuilderObject.SetField("StreetID", StreetID);
                QueryBuilderObject.SetField("CustomerTypeID", Credit.ToString()); //HardCoded -1- Cash -2- Credit
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("OnHold", OnHold);
                QueryBuilderObject.SetField("OrganizationID", OrgID);

                QueryBuilderObject.SetField("StreetAddress", "0");
                QueryBuilderObject.SetField("InActive", InActive);
                QueryBuilderObject.SetField("Notes", "0");
                QueryBuilderObject.SetField("SkipCreditCheck", "0");
                if (Credit == 2)
                {
                    QueryBuilderObject.SetField("PaymentTermID", PaymentTermID);
                }
                else
                {
                    QueryBuilderObject.SetField("PaymentTermID", "0");
                }
                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("CustomerOutlet", db_vms);
            }

          //  InCubeQuery DeleteCustomerGroup = new InCubeQuery(db_vms, "Delete From CustomerOutletGroup Where groupid<1000 CustomerID = " + CustomerID + " AND OutletID = " + OutletID);
           // DeleteCustomerGroup.ExecuteNonQuery();

          //  err = ExistObject("CustomerOutletGroup", "GroupID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
           // if (err != InCubeErrors.Success)
          //  {
            if (GroupID != "0")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", GroupID.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            if (GroupID2 != "0")
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("GroupID", GroupID2.ToString());
                QueryBuilderObject.InsertQueryString("CustomerOutletGroup", db_vms);
            }
            //}
            //else
            //{
            //    if (GroupID != "0")
            //    {
            //        QueryBuilderObject.SetField("GroupID", GroupID.ToString());
            //        QueryBuilderObject.UpdateQueryString("CustomerOutletGroup", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID, db_vms);
            //    }
            //}

            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 1", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish + "'");
                QueryBuilderObject.SetField("Address", "'" + CustomerAddressEnglish + "'");
                QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            ExistCustomer = GetFieldValue("CustomerOutletLanguage", "OutletID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms); //Arabic
            if (ExistCustomer != string.Empty)
            {
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                QueryBuilderObject.UpdateQueryString("CustomerOutletLanguage", "  CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " AND LanguageID = 2", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic + "'");
                QueryBuilderObject.SetField("Address", "N'" + CustomerAddressArabic + "'");
                QueryBuilderObject.InsertQueryString("CustomerOutletLanguage", db_vms);
            }

            #region Customer Outlet Account
            int AccountID = 1;

            string MainCustomerAccount = GetFieldValue("AccountCust", "AccountID", "CustomerID = " + CustomerID, db_vms);

            ExistCustomerAccount = GetFieldValue("AccountCustOut inner join account on AccountCustOut.accountid=account.accountid", "account.AccountID", "CustomerID = " + CustomerID + " AND OutletID = " + OutletID + " and OrganizationID=" + OrgID, db_vms);
            if (ExistCustomerAccount == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "1");//customer account
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance); // Balance =  Balance + ChqNotCollected
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("CurrencyID", "1");
                QueryBuilderObject.SetField("ParentAccountID", MainCustomerAccount);

                QueryBuilderObject.InsertQueryString("Account", db_vms);

                QueryBuilderObject.SetField("CustomerID", CustomerID.ToString());
                QueryBuilderObject.SetField("OutletID", OutletID);
                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.InsertQueryString("AccountCustOut", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + CustomerDescriptionEnglish.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + CustomerDescriptionArabic.Trim() + " Account'");
                QueryBuilderObject.InsertQueryString("AccountLanguage", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("ParentAccountID", MainCustomerAccount);
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance); // Balance =  Balance + ChqNotCollected
                QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + ExistCustomerAccount, db_vms);
            }
            #endregion

            #endregion
        }

        #endregion

        #region UpdateEmployee

        public override void UpdateSalesPerson()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;


                //UpdateBanks();

                InCubeErrors err;
                object field = new object();

                string SelectItems = @"SELECT   [EmployeeCode]
      ,[EmployeeName]
      ,[Phone]
      ,[CreditLimit]
      ,[Balance]
      ,[DivisionCode]
      ,[OrganizationCode]
  FROM  [invan_Salesman]
";

                DefaultOrganization();

                //DataTable DT = new DataTable();

                //OdbcCommand CMD = new OdbcCommand(SelectItems, Conn);
                //OdbcDataAdapter DA = new OdbcDataAdapter(CMD);

                //DA.Fill(DT);
                //DA.Dispose();

                InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
                ItemQuery.Execute();

                DataTable DT = new DataTable();

                DT = ItemQuery.GetDataTable();

                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    ReportProgress("Updating Employee");
 

                    string EmployeeCode = DT.Rows[i][0].ToString();

                    string EmployeeNameA = DT.Rows[i][1].ToString();

                    string EmployeeNameE = DT.Rows[i][1].ToString();

                    string OrgCode = DT.Rows[i][6].ToString();

                    string OrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + OrgCode + "'", db_vms);
                    if (OrgID == "") OrgID = "1";

                    string CreditLimit = DT.Rows[i][3].ToString();

                    string Balance = DT.Rows[i][4].ToString();

                    if (CreditLimit.Equals(string.Empty))
                    {
                        CreditLimit = "0";
                    }

                    if (Balance.Equals(string.Empty))
                    {
                        Balance = "0";
                    }

                    if (EmployeeCode == string.Empty)
                        continue;

                    string SalespersonID = "";

                    SalespersonID = GetFieldValue("Employee", "EmployeeID", "Employeecode = '" + EmployeeCode + "'", db_vms);
                    if (SalespersonID == string.Empty)
                    {
                        SalespersonID = GetFieldValue("Employee", "isnull(MAX(EmployeeID),0) + 1", db_vms);
                    }


                    string DivisionID = "";
                    AddUpdateSalesperson(SalespersonID, EmployeeCode, EmployeeNameA, EmployeeNameE, "0", ref TOTALUPDATED, ref TOTALINSERTED, DivisionID, CreditLimit, Balance, OrgID);
                  //  AddUpdateWarehouse(SalespersonID, EmployeeCode, EmployeeCode, " ", SalespersonID, EmployeeCode, ref TOTALUPDATED, ref TOTALINSERTED, "2", OrgID);
                }//while

                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< SALESPERSON >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                //Conn.Close();
            }
        }

        private void AddUpdateSalesperson(string SalespersonID, string SalespersonCode, string SalespersonNameArabic, string SalespersonNameEnglish, string Phone, ref int TOTALUPDATED, ref int TOTALINSERTED, string DivisionID, string CreditLimit, string Balance, string OrgID)
        {
            string ExistEmployee = "";
            string ExitAccount = "";

            ExistEmployee = GetFieldValue("Employee", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)// New Salesperon --- Insert Query
            {
                TOTALINSERTED++;

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("Phone", "'" + Phone + "'");
                QueryBuilderObject.SetField("EmployeeCode", "'" + SalespersonCode + "'");
                QueryBuilderObject.SetField("NationalIDNumber", "0");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("InActive", "0");
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("EmployeeTypeID", "2");
                //QueryBuilderObject.SetField("EmployeeTypeID", "2");

                QueryBuilderObject.SetField("CreatedBy", UserID.ToString());
                QueryBuilderObject.SetField("CreatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");

                QueryBuilderObject.InsertQueryString("Employee", db_vms);
            }
            else
            {
                QueryBuilderObject.SetField("OnHold", "0");
                QueryBuilderObject.SetField("UpdatedBy", UserID.ToString());
                QueryBuilderObject.SetField("UpdatedDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                err = QueryBuilderObject.UpdateQueryString("Employee", "EmployeeID = " + SalespersonID, db_vms);
            }

            ExistEmployee = GetFieldValue("EmployeeLanguage", "EmployeeID", "EmployeeID = " + SalespersonID + " AND LanguageID = 1", db_vms);
            if (ExistEmployee != string.Empty)
            {
                TOTALUPDATED++;
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
                TOTALUPDATED++;
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

            ExistEmployee = GetFieldValue("Operator", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OperatorName", "'" + SalespersonCode + "'");

                QueryBuilderObject.SetField("FrontOffice", "1");
                QueryBuilderObject.InsertQueryString("Operator", db_vms);
            }

            InCubeQuery CMD = new InCubeQuery(db_vms, "Select DivisionID From Division where OrganizationID=" + OrgID);
            CMD.Execute();
            DataTable DT = CMD.GetDataTable();
            CMD.Close();

            for (int i = 0; i < DT.Rows.Count; i++)
            {
                string _divisionID = DT.Rows[i][0].ToString();

                ExistEmployee = GetFieldValue("EmployeeDivision", "EmployeeID", "EmployeeID = " + SalespersonID + " AND DivisionID = " + _divisionID, db_vms);
                if (ExistEmployee == string.Empty)
                {
                    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                    QueryBuilderObject.SetField("DivisionID", _divisionID);
                    QueryBuilderObject.InsertQueryString("EmployeeDivision", db_vms);
                }
            }

            DT.Dispose();

            ExistEmployee = GetFieldValue("EmployeeOrganization", "EmployeeID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)
            {
                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("OrganizationID", OrganizationID);
                QueryBuilderObject.InsertQueryString("EmployeeOrganization", db_vms);
            }

            int AccountID = 1;

            ExistEmployee = GetFieldValue("AccountEmp", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
            if (ExistEmployee == string.Empty)
            {
                AccountID = int.Parse(GetFieldValue("Account", "isnull(MAX(AccountID),0) + 1", db_vms));

                QueryBuilderObject.SetField("AccountID", AccountID.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");//employee account   
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
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
            else
            {

                //update
                // ExitAccount = GetFieldValue("Account", "AccountID", "EmployeeID = " + SalespersonID, db_vms);
                QueryBuilderObject.SetField("AccountID", ExistEmployee.ToString());
                QueryBuilderObject.SetField("AccountTypeID", "2");//employee account   
                QueryBuilderObject.SetField("CreditLimit", CreditLimit);
                QueryBuilderObject.SetField("Balance", Balance);
                QueryBuilderObject.SetField("GL", "0");
                QueryBuilderObject.SetField("OrganizationID", OrgID);
                QueryBuilderObject.SetField("CurrencyID", "1");

                InCubeErrors err = QueryBuilderObject.UpdateQueryString("Account", "AccountID = " + ExistEmployee, db_vms);

                QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                QueryBuilderObject.SetField("AccountID", ExistEmployee.ToString());
                QueryBuilderObject.UpdateQueryString("AccountEmp", db_vms);

                QueryBuilderObject.SetField("AccountID", ExistEmployee.ToString());
                QueryBuilderObject.SetField("LanguageID", "1");
                QueryBuilderObject.SetField("Description", "'" + SalespersonNameEnglish.Trim() + " Account'");
                QueryBuilderObject.UpdateQueryString("AccountLanguage", db_vms);

                QueryBuilderObject.SetField("AccountID", ExistEmployee.ToString());
                QueryBuilderObject.SetField("LanguageID", "2");
                QueryBuilderObject.SetField("Description", "N'" + SalespersonNameArabic.Trim() + " Account'");
                QueryBuilderObject.UpdateQueryString("AccountLanguage", db_vms);

            }
        }

        #endregion

        #region UpdateWarehouse
        public override void UpdateMainWarehouse()
        {
            try
            {
                int TOTALUPDATED = 0;
                int TOTALINSERTED = 0;


                InCubeErrors err;
                object field = new object();

                //if (Conn.State == ConnectionState.Closed)
                //    Conn.Open();

                //if (Conn.State != ConnectionState.Open)
                //{
                //   WriteMessage("\r\n");
                //   WriteMessage("Cannot connect to Intermediate database , please check the connection");
                //    return;
                //}

                string SelectItems = @"SELECT   [Warehousecode]
       ,[Description]
      ,[type]
      ,[OrganizationCode]
  FROM  [invan_Warehouse]";

                DefaultOrganization();

                //DataTable DT = new DataTable();

                //OdbcCommand CMD = new OdbcCommand(SelectItems, Conn);
                //OdbcDataAdapter DA = new OdbcDataAdapter(CMD);

                //DA.Fill(DT);
                //DA.Dispose();

                InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectItems);
                ItemQuery.Execute();

                DataTable DT = new DataTable();

                DT = ItemQuery.GetDataTable();

                ClearProgress();
                SetProgressMax(DT.Rows.Count);
                for (int i = 0; i < DT.Rows.Count; i++)
                {
                    ReportProgress("Updating WH");

                    string WarehouseCode = DT.Rows[i][0].ToString();

                    string WarehouseDescription = DT.Rows[i][1].ToString();

                    string SalesmanCode = "";

                    string WarehouseType = DT.Rows[i][2].ToString();

                    string OrgCode = DT.Rows[i][3].ToString();

                    string OrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + OrgCode + "'", db_vms);
                    if (OrgID == "") OrgID = "1";

                    if (WarehouseCode == string.Empty)
                        continue;

                    string Address = "";
                    string WarehouseID = "";
                    string VehicleRegNum = "";

                    WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode='" + WarehouseCode + "'", db_vms);
                    if (WarehouseID == string.Empty)
                    {
                        WarehouseID = GetFieldValue("Warehouse", "isnull(MAX(WarehouseID),0) + 1", db_vms);
                    }

                    AddUpdateWarehouse(WarehouseID, WarehouseCode, WarehouseDescription, Address, VehicleRegNum, SalesmanCode, ref TOTALUPDATED, ref TOTALINSERTED, WarehouseType, OrgID);

                }//while

                DT.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< WAREHOUSE >>> Total Updated = " + TOTALUPDATED + " , Total Inserted = " + TOTALINSERTED);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                //Conn.Close();
            }
        }

        private void AddUpdateWarehouse(string WarehouseID, string WarehouseCode, string WarehouceName, string Address, string VehicleRegNum, string SalesmanCode, ref int TOTALUPDATED, ref int TOTALINSERTED, string WarehouseType, string OrgID)
        {


            string ExitWarehouse = "";

            ExitWarehouse = GetFieldValue("Warehouse", "WarehouseID", "WarehouseID = " + WarehouseID, db_vms);
            if (ExitWarehouse != string.Empty) // Exist Warehouse --- Update Query
            {
                TOTALUPDATED++;

                QueryBuilderObject.SetField("Phone", "''");
                QueryBuilderObject.SetField("WarehouseCode", "'" + WarehouseCode + "'");
                //QueryBuilderObject.SetField("OrganizationID", OrganizationID);
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

                InCubeErrors err = QueryBuilderObject.InsertQueryString("Warehouse", db_vms);
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
            if (WarehouseType == "2")//Van
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
            }// if type =2
            #endregion
        }

        #endregion

        #region UpdateStock

        public override void UpdateStock()
        {
            //UpdateStockForWarehouse(UpdateAll,WarehouseID);


            if (Filters.WarehouseID==-1)
            {
                InCubeQuery query = new InCubeQuery(db_vms, "select WarehouseID from Warehouse where WarehouseTypeID=2");
                DataTable DT = new DataTable();
                err = query.Execute();
                if (err == InCubeErrors.Success)
                {
                    DT = query.GetDataTable();
                    foreach (DataRow row in DT.Rows)
                    {
                        UpdateStockForWarehouse(false, row["WarehouseID"].ToString());
                    }
                }
            }
            else
            {
                UpdateStockForWarehouse(false, Filters.WarehouseID.ToString());
            }

        }

        protected bool IsVehicleUploaded(string vehicleID, InCubeDatabase database)
        {
            string Uploaded = GetFieldValue("RouteHistory", "Uploaded", " RouteHistoryID = (SELECT MAX(RouteHistoryID) FROM RouteHistory WHERE VehicleID = " + vehicleID + ")  ", database);

            if (Uploaded.Trim() != string.Empty && bool.Parse(Uploaded.Trim()))
            {
                return true;
            }
            return false;


        }

        private void UpdateStockForWarehouse(bool UpdateAll, string _warehouseID)
        {
            try
            {
                int TOTALUPDATED = 0;
                InCubeErrors err;
                object field = new object();

                #region Update Stock

                //if (Conn.State == ConnectionState.Closed)
                //    Conn.Open();
                //if (Conn.State != ConnectionState.Open)
                //{
                //   WriteMessage("\r\n");
                //   WriteMessage("Cannot connect to Intermediate database , please check the connection");
                //    return;
                //}

                string DeleteStock = "";
                string SelectWarehouses = "";

                if (UpdateAll)
                {
                    DeleteStock = "delete from WarehouseStock";
                    SelectWarehouses = @"SELECT  [WarehouseCode]
      ,[ItemCode]
      ,[Batch]
      ,[Quantity]
      ,[ExpiryDate]
      ,[invanCode]
      ,[UOM]
  FROM [invan_WarehouseStockBalance] ";
                }
                else
                {
                    string WarehouseCode = GetFieldValue("Warehouse", "WarehouseCode", " WarehouseID = " + _warehouseID, db_vms);
                    if (IsVehicleUploaded(_warehouseID, db_vms))
                    {
                        WriteMessage("\r\n");
                        WriteMessage("<<< you cant update the stock for vehicle " + WarehouseCode + " because it is uploaded    >>> Total Updated = " + TOTALUPDATED);
                        return;
                    }
                    DeleteStock = "delete from WarehouseStock Where WarehouseID = " + _warehouseID;
                    SelectWarehouses = @"SELECT  [WarehouseCode]
      ,[ItemCode]
      ,[Batch]
      ,[Quantity]
      ,[ExpiryDate]
      ,[invanCode]
      ,[UOM]
  FROM [invan_WarehouseStockBalance] Where WarehouseCode = '" + WarehouseCode + "'";
                }
                this.QueryBuilderObject.RunQuery(DeleteStock, base.db_vms);
                DefaultOrganization();

                DataTable dtWarehouses = new DataTable();
                InCubeQuery WHQuery = new InCubeQuery(db_ERP, SelectWarehouses);
                WHQuery.Execute();
          
                dtWarehouses = WHQuery.GetDataTable();
                WHQuery = new InCubeQuery(db_ERP, DeleteStock);
                WHQuery.ExecuteNonQuery();

                ClearProgress();
            SetProgressMax(dtWarehouses.Rows.Count);
                for (int i = 0; i < dtWarehouses.Rows.Count; i++)
                {
                    ReportProgress("Updating Stock");
                    TOTALUPDATED++;

                    string VanCode = dtWarehouses.Rows[i][0].ToString();

                    string ItemCode = dtWarehouses.Rows[i][5].ToString();

                    string UOM = dtWarehouses.Rows[i][6].ToString();

                    string Quantity = dtWarehouses.Rows[i][3].ToString();

                    string Batch = dtWarehouses.Rows[i][2].ToString();

                    string ExpiryDate = dtWarehouses.Rows[i][4].ToString();

                    if (VanCode == string.Empty)
                        continue;

                    if (Batch == string.Empty)
                    {
                        Batch = "NoBatch";
                    }

                    try
                    {
                        ExpiryDate = DateTime.Parse(ExpiryDate, EsES).ToString(DateFormat);
                    }
                    catch
                    {
                        ExpiryDate = DateTime.Now.AddDays(10).ToString(DateFormat);
                    }

                    string WarehouseID = GetFieldValue("Warehouse", "WarehouseID", "WarehouseCode = '" + VanCode + "'", db_vms);
                    string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                    string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOM + "'", db_vms);
                    string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID + " AND PackTypeID = " + PackTypeID, db_vms);

                    if (WarehouseID == string.Empty)
                    {
                        continue;
                    }

                    if (ItemID == string.Empty)
                    {
                        continue;
                    }

                    TOTALUPDATED++;

                    string query = "Select PackID from Pack where ItemID = " + ItemID; // if no packtypeid (UOM) defined in pack for this item 
                    InCubeQuery CMD = new InCubeQuery(query, db_vms);
                    CMD.Execute();
                    err = CMD.FindFirst();

                    while (err == InCubeErrors.Success)
                    {
                        CMD.GetField(0, ref field);
                        string _packid = field.ToString();

                        string _quantity = "0";

                        err = ExistObject("WarehouseStock", "PackID", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                            QueryBuilderObject.SetField("ZoneID", "1");
                            QueryBuilderObject.SetField("PackID", _packid);
                            QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
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

                        err = ExistObject("DailyWarehouseStock", "PackID", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + _packid + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                        if (err != InCubeErrors.Success)
                        {
                            QueryBuilderObject.SetField("WarehouseID", WarehouseID);
                            QueryBuilderObject.SetField("ZoneID", "1");
                            QueryBuilderObject.SetField("PackID", _packid);
                            QueryBuilderObject.SetField("ExpiryDate", "'" + ExpiryDate + "'");
                            QueryBuilderObject.SetField("StockDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("BatchNo", "'" + Batch + "'");

                            if (_packid == PackID)
                            {
                                QueryBuilderObject.SetField("Quantity", Quantity);
                            }
                            else
                            {
                                QueryBuilderObject.SetField("Quantity", _quantity);
                            }

                            QueryBuilderObject.SetField("ProductionDate", "'" + DateTime.Now.ToString(DateFormat) + "'");
                            QueryBuilderObject.SetField("SampleQuantity", "0");
                            QueryBuilderObject.InsertQueryString("DailyWarehouseStock", db_vms);
                        }
                        else if (_packid == PackID)
                        {
                            QueryBuilderObject.SetField("Quantity", Quantity);
                            QueryBuilderObject.UpdateQueryString("DailyWarehouseStock", "WarehouseID = " + WarehouseID + " AND ZoneID = 1 AND PackID = " + PackID + " AND ExpiryDate = '" + ExpiryDate + "' AND BatchNo = '" + Batch + "'", db_vms);
                        }

                        err = CMD.FindNext();
                    }
                }

                dtWarehouses.Dispose();
                WriteMessage("\r\n");
                WriteMessage("<<< STOCK Updated >>> Total Updated = " + TOTALUPDATED);

                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
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
            string query = @"SELECT   [PriceListCode]
      ,[PriceName]
      ,[Price]
      ,[Validfrom]
      ,[ValidTo]
      ,[SC01002]
      ,[SC01094] ItemCode
  FROM  [invan_PriceList]";

            DataTable DT_Price = new DataTable();
            InCubeQuery incubeQuery;
            incubeQuery = new InCubeQuery(db_ERP, query);
            err = incubeQuery.Execute();
            if (err == InCubeErrors.Success)
            {
                DT_Price = incubeQuery.GetDataTable();
            }


              ClearProgress();
            SetProgressMax(DT_Price.Rows.Count);
            foreach (DataRow row in DT_Price.Rows)
            {
                ReportProgress("Updating Price lists");
 
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

                string PriceListCode = row[0].ToString().Trim();
                string IsDefaultPricelist = "0";
                string PriceListDescription = row[1].ToString().Trim();
                string orgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + row[3].ToString().Trim() + "'", db_vms);
                if (PriceListCode == string.Empty)
                    continue;



                string PriceListID = "1";

                err = ExistObject("PriceList", "PriceListID", " PriceListCode = '" + PriceListCode + "'", db_vms);
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

                if (IsDefaultPricelist == "1" || IsDefaultPricelist == "X")
                {
                    QueryBuilderObject.SetField("KeyValue", PriceListID);
                    QueryBuilderObject.UpdateQueryString("Configuration", "KeyName = 'DefaultPriceListID' AND EmployeeID = -1", db_vms);
                }

                err = ExistObject("PriceQuantityRange", "PriceQuantityRangeID", " PriceQuantityRangeID = 1", db_vms);
                if (err != InCubeErrors.Success)
                {
                    QueryBuilderObject.SetField("PriceQuantityRangeID", "1");
                    QueryBuilderObject.SetField("RangeStart", "0");
                    QueryBuilderObject.SetField("RangeEnd", "9999999");
                    QueryBuilderObject.InsertQueryString("PriceQuantityRange", db_vms);
                }


                // PLDID =; GetFieldValue("PriceList", "PriceListID", " PriceListCode = '" + PLDID + "'", db_vms);

                string ItemCode = row[6].ToString().Trim();
                string Price = row[2].ToString().Trim();
                if (string.IsNullOrEmpty(Price)) Price = "0";
                string Tax = "16";
                string ItemID = GetFieldValue("Item", "ItemID", "ItemCode = '" + ItemCode + "'", db_vms);
                Price = (decimal.Parse(Price) / 1.16M).ToString();
                if (ItemID == string.Empty)
                {
                    continue;
                }
                string CurrencyID = "1";// GetFieldValue("Currency", "CurrencyID", "Code = '" + Currancy + "'", db_vms);

              
              //  string PackTypeID = GetFieldValue("PackTypeLanguage", "PackTypeID", " LanguageID = 1 and Description = '" + UOMdesc + "'", db_vms);
                string PackID = GetFieldValue("Pack", "PackID", "ItemID = " + ItemID  , db_vms);

                // int PriceDefinitionID = 1;

                string PriceDefinitionID = GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PriceListID, db_vms);
                if (PriceDefinitionID.Equals(string.Empty))
                {
                    PriceDefinitionID = GetFieldValue("PriceDefinition", "ISNULL(MAX(PriceDefinitionID),0) + 1", db_vms);

                    QueryBuilderObject.SetField("PriceDefinitionID", PriceDefinitionID.ToString());
                    QueryBuilderObject.SetField("QuantityRangeID", "1");
                    QueryBuilderObject.SetField("PackID", PackID);
                    QueryBuilderObject.SetField("CurrencyID", CurrencyID);
                    QueryBuilderObject.SetField("Tax", Tax);
                    QueryBuilderObject.SetField("Price", Price);
                    QueryBuilderObject.SetField("PriceListID", PriceListID);
                    QueryBuilderObject.InsertQueryString("PriceDefinition", db_vms);
                }
                else
                {
                    //  PriceDefinitionID = int.Parse(GetFieldValue("PriceDefinition", "PriceDefinitionID", "PackID = " + PackID + " AND PriceListID = " + PLDID, db_vms));


                    QueryBuilderObject.SetField("Tax", Tax);
                    QueryBuilderObject.SetField("Price", Price);
                    QueryBuilderObject.UpdateQueryString("PriceDefinition", "PackID = " + PackID + " AND PriceListID = " + PriceListID + " AND PriceDefinitionID = " + PriceDefinitionID, db_vms);

                }
                  string GroupId = GetFieldValue("CustomerGroup", "GroupId", "GroupCode = 'P" + PriceListCode , db_vms);
                  string PriceID = GetFieldValue("GroupPrice", "PriceListID", "PriceListID = " + PriceListID + " AND GroupId = " + GroupId, db_vms);
                  if (!GroupId.Equals(string.Empty) && PriceID.Equals(string.Empty))
                  {
                      QueryBuilderObject.SetField("PriceListID", PriceListID);
                      QueryBuilderObject.SetField("GroupId", GroupId);
                      QueryBuilderObject.InsertQueryString("GroupPrice",      db_vms);

                  }
               

            }

         
        }
        #endregion

        #region UpdateInvoiceHistory

//        public override void UpdateInvoiceHistory()
//        {

//            UpdateCreditInvoiceAmount();

//            int TOTALUPDATED = 0;
//            int TOTALINSERTED = 0;

//            InCubeErrors err;
//            InCubeErrors err1;

//            object field = new object();

//            #region Get Invoices

//            string SelectInvoices = "";

//            #region Query

//            //SelectInvoices = @"Select [Invoice Number],[Invoice Date],[Customer Code],[Sales Person],[Total Amount],[Remaining Amount],InCube  From  VScalaInvoice01";
//            SelectInvoices = @"SELECT [Invoice Number], [Invoice Date], [Customer Code], [Sales Person], [Total Amount], [Remaining Amount], [InCube Reference], [Customer Name], LastPaymentDate,OrgCode
//FROM   VScalaInvoice01 left outer join [Transaction] on TransactionID=(Case when isnull([InCube Reference],'')='' or isnull([InCube Reference],'')=' ' then [Invoice Number] else [InCube Reference] end collate arabic_bin) and OrganizationID=cast(right(OrgCode,1) as int)
//where TransactionID is null
//";

//            #endregion

//            //DataTable DT = new DataTable();

//            //OdbcCommand CMD = new OdbcCommand(SelectInvoices, Conn);
//            //OdbcDataAdapter DA = new OdbcDataAdapter(CMD);

//            //DA.Fill(DT);
//            //DA.Dispose();

//            InCubeQuery ItemQuery = new InCubeQuery(db_ERP, SelectInvoices);
//            ItemQuery.Execute();

//            DataTable DT = new DataTable();

//            DT = ItemQuery.GetDataTable();
//            ClearProgress();
//            SetProgressMax(DT.Rows.Count);
//            for (int i = 0; i < DT.Rows.Count; i++)
//            { 
// ReportProgress("Updating Invoices");
             
//                string ReferenceNo = DT.Rows[i][0].ToString();//TRANSACTION_NUMBER
//                DateTime Date = DateTime.Parse(DT.Rows[i][1].ToString());//TRANSACTION_DATE
//                string CustomerNumber = DT.Rows[i][2].ToString();//CUSTOMERCODE
//                string SalesID = DT.Rows[i][3].ToString();//SALESPERSON_CODE
//                string Total = DT.Rows[i][4].ToString();//TOTAL_AMOUNT
//                string Ramount = DT.Rows[i][5].ToString();//REMAINING_AMOUNT
//                string DocNumber = DT.Rows[i][6].ToString();//REMAINING_AMOUNT


//                string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerNumber + "'", db_vms);
//                string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + CustomerNumber + "'", db_vms);
//                string Note = ReferenceNo;

//                if (CustomerID == string.Empty)
//                {
//                    continue;
//                }

//                string SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms);

//                if (SalespersonID == string.Empty)
//                {
//                    continue;
//                }

//                if (DocNumber.Trim().ToString() == "")
//                {
//                    DocNumber = ReferenceNo;
//                }
//                err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "'", db_vms);
//                if (err != InCubeErrors.Success)
//                {
//                    TOTALINSERTED++;

//                    QueryBuilderObject.SetField("CustomerID", CustomerID);
//                    QueryBuilderObject.SetField("OutletID", OutletID);
//                    QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
//                    QueryBuilderObject.SetField("EmployeeID", SalespersonID);
//                    QueryBuilderObject.SetField("TransactionDate", "'" + Date.ToString("dd/MMM/yyyy") + "'");

//                    //if (TranType == "1") // Invoice
//                    QueryBuilderObject.SetField("TransactionTypeID", "1");
//                    //else if (TranType == "3") // Debit Note
//                    //    QueryBuilderObject.SetField("TransactionTypeID", "6");

//                    QueryBuilderObject.SetField("Discount", "0");
//                    QueryBuilderObject.SetField("Synchronized", "1");
//                    QueryBuilderObject.SetField("RemainingAmount", Ramount);
//                    QueryBuilderObject.SetField("Grosstotal", Total);
//                    QueryBuilderObject.SetField("Nettotal", Total);
//                    QueryBuilderObject.SetField("Notes", "'" + Note + "'");//add refrenc number to invoices from SCALA


//                    //if (VOIDED == "1")
//                    //{
//                    //    QueryBuilderObject.SetField("TransactionStatusID", "5");
//                    //    QueryBuilderObject.SetField("Voided", "1");
//                    //}
//                    //else
//                    //{
//                    QueryBuilderObject.SetField("TransactionStatusID", "1");
//                    //}

//                    QueryBuilderObject.SetField("Posted", "1");
//                    QueryBuilderObject.SetField("DivisionID", "-1");
//                    QueryBuilderObject.SetField("SalesMode", "2");

//                    err = QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);
//                }
//                else if (err == InCubeErrors.Success)
//                {
//                    TOTALUPDATED++;

//                    QueryBuilderObject.SetField("RemainingAmount", Ramount);
//                    QueryBuilderObject.SetField("Notes", "'" + Note + "'");//add refrenc number to invoices from SCALA
//                    QueryBuilderObject.SetField("SalesMode", "2");

//                    //if (VOIDED == "1")
//                    //{
//                    //    QueryBuilderObject.SetField("TransactionStatusID", "5");
//                    //    QueryBuilderObject.SetField("Voided", "1");
//                    //}

//                    QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID ='" + DocNumber + "'", db_vms);
//                }

//            }

//            WriteMessage("\r\n");
//            WriteMessage("<<< INVOICES >>> Total Inserted = " + TOTALINSERTED + " Total Updated = " + TOTALUPDATED);

//            #endregion

//        }

        #endregion

        #region UpdateInvoice
             
        public override void OutStanding ()
        {


            this.DefaultOrganization();
            int TOTALUPDATED = 0;
            int TOTALINSERTED = 0;

            InCubeErrors err;
            InCubeErrors err1;

            object field = new object();

            #region Get Invoices

            DataTable DT = new DataTable();
            #region Query
            string cmdText = @"SELECT  [CustAccount]
      ,[TransDate]
      ,[Amount]
      ,[RemainingAmount]
      ,[INVOICE]
      ,[Tax]
      ,[InvoiceOrderNumber]
      ,[Reference]
  FROM IntegrationDB.[dbo].[invan_CustOpenTrans]
  where INVOICE<>''";
            InCubeQuery incubeQuery = new InCubeQuery(db_ERP, cmdText);
            incubeQuery.Execute();
            DT = incubeQuery.GetDataTable();
            if (DT == null || DT.Rows.Count == 0)
            {
                WriteMessage("No Invoices data found\r\n");

                return;
            }


            #endregion
             
            WriteMessage("<<< Updating Invoices >>>  ");

            if (DT.Rows.Count > 0)
            {
                string sql = "update [Transaction] set RemainingAmount=0 where  Synchronized=1 and TransactionTypeID in(1,3,6,5) and RemainingAmount>0 and Voided<>1 ";
                InCubeQuery query = new InCubeQuery(sql, db_vms);
                query.ExecuteNonQuery();
            }
             ClearProgress();
            SetProgressMax(DT.Rows.Count);
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                try
                {
 ReportProgress("Updating Invoices");
                     
                    //  string TranType = DT.Rows[i][0].ToString();//TRANSACTION_TYPE
                    string DocNumber = DT.Rows[i]["INVOICE"].ToString();//TRANSACTION_NUMBER
                    DateTime Date = DateTime.Parse(DT.Rows[i]["TransDate"].ToString());//TRANSACTION_DATE
                    string CustomerNumber = DT.Rows[i]["CustAccount"].ToString();//CUSTOMERCODE
                    string OutletNumber = DT.Rows[i]["CustAccount"].ToString();//CUSTOMERCODE
                    string SalesID = DT.Rows[i]["Reference"].ToString();//SALESPERSON_CODE
                    string Total = DT.Rows[i]["Amount"].ToString();//TOTAL_AMOUNT
                    string Ramount = DT.Rows[i]["RemainingAmount"].ToString();//REMAINING_AMOUNT
                    string Tax = DT.Rows[i]["Tax"].ToString();//REMAINING_AMOUNT
                    string InVanOrderNumber = DT.Rows[i]["InvoiceOrderNumber"].ToString();//REMAINING_AMOUNT
                    string DivisionID = "-1";
                    // string Note = DT.Rows[i]["BatchNo"].ToString();//VOIDED

                    string OrgID = "1";// GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + CompanyID + "'", db_vms);



                    string CustomerID = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerNumber + "'", db_vms);
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerCode = '" + OutletNumber + "' and CustomerID=" + CustomerID, db_vms);
                    string AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " and OutletID=" + OutletID, db_vms);

                    if (CustomerID == string.Empty || OutletID == string.Empty || AccountID == string.Empty)
                    {
                        continue;
                    }
                    string SalespersonID = "0";
                    if (SalesID != "")
                    {
                        SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms);

                        if (SalespersonID == string.Empty)
                        {
                            continue;
                        }
                    }
                    // string RouteID = GetFieldValue("EmployeeTerritory", "TerritoryID", "EmployeeID = " + SalespersonID, db_vms);


                    err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + InVanOrderNumber + "' and CustomerID=" + CustomerID, db_vms);
                    if (err != InCubeErrors.Success)
                    {
                         err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms);
                         if (err != InCubeErrors.Success)
                         {
                             TOTALINSERTED++;
                             QueryBuilderObject = new QueryBuilder();
                             QueryBuilderObject.SetField("CustomerID", CustomerID);
                             QueryBuilderObject.SetField("OutletID", OutletID);
                             QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                             QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                             QueryBuilderObject.SetDateField("TransactionDate", Date);
                             QueryBuilderObject.SetField("TransactionTypeID", decimal.Parse(Total) > 1 ? "1" : "5");
                             QueryBuilderObject.SetField("Discount", "0");
                             QueryBuilderObject.SetField("Synchronized", "1");
                             QueryBuilderObject.SetField("RemainingAmount", Ramount.Replace("-", ""));
                             QueryBuilderObject.SetField("Grosstotal", Total);
                             QueryBuilderObject.SetField("Nettotal", Total);
                             QueryBuilderObject.SetField("Tax", Tax);
                             QueryBuilderObject.SetField("Posted", "1");
                             QueryBuilderObject.SetField("DivisionID", "-1");
                             QueryBuilderObject.SetField("AccountID", AccountID);
                             QueryBuilderObject.SetField("OrganizationID", OrgID);
                             QueryBuilderObject.SetField("RefOrderID", "'" + InVanOrderNumber + "'");

                             QueryBuilderObject.InsertQueryString("[Transaction]", db_vms);
                         }
                         else if (err == InCubeErrors.Success)
                         {
                             TOTALUPDATED++;

                             QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                             QueryBuilderObject.SetField("RemainingAmount", Ramount.Replace("-", ""));
                             // QueryBuilderObject.SetField("Notes", "'" + Note + "'");
                             QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms);
                         }
                    }
                    else if (err == InCubeErrors.Success)
                    {
                        TOTALUPDATED++;

                        QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                        QueryBuilderObject.SetField("RemainingAmount", Ramount.Replace("-", ""));
                        // QueryBuilderObject.SetField("Notes", "'" + Note + "'");
                        QueryBuilderObject.UpdateQueryString("[Transaction]", "TransactionID ='" + InVanOrderNumber + "' and CustomerID=" + CustomerID, db_vms);
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "  " + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);

                }
            }

      

            
            WriteMessage("<<< INVOICES >>> Total Inserted = " + TOTALINSERTED + " Total Updated = " + TOTALUPDATED);

            #endregion

        }
        public void UpdateCreditInvoiceAmount()
        {
            try
            {
                // string str = "update [Transaction] set RemainingAmount=0 where Voided=0 and RemainingAmount>0 and TransactionTypeID in(1,3) and Synchronized=1";
                string str = @"update trn set trn.RemainingAmount=0 from

[Transaction] trn left outer join 
(
SELECT Case when isnull([InCube Reference],'')='' or isnull([InCube Reference],'')=' ' then [Invoice Number] else [InCube Reference] end collate arabic_bin as InVanRef,case [OrgCode] when '01' then 1 else 2 end as OrgID
FROM VScalaInvoice01
)q on TransactionID=q.InVanRef and OrganizationID=q.OrgID
where RemainingAmount<>0 and Voided=0 and Synchronized=1 and q.InVanRef is null
";
                //  collate Arabic_BIN 
                InCubeQuery query = new InCubeQuery(db_vms, str);
                err = query.ExecuteNonQuery();

                //Query to update the remaining invoice balances (provided by Nestle)
                // str = @" Update[Transaction]set RemainingAmount=[Remaining Amount]FROM [VScalaInvoice01]where (TransactionID=[InCube Reference]  collate arabic_bin or TransactionID=[Invoice Number] collate arabic_bin)and RemainingAmount<>[Remaining Amount] andTransactionID in (SELECT TOP (100) PERCENT q2.TransactionIDFROM(SELECT TransactionID, CONVERT(date,MAX(PaymentDate)) AS lastInVanPayDate    FROM CustomerPayment    WHERE PaymentStatusID<>5    GROUP BY TransactionID) AS q3 RIGHT OUTER JOIN(SELECT  CASE WHEN [InCube Reference] LIKE 'INV-%' OR [InCube Reference] LIKE 'OrdInv-%' THEN [InCube Reference] ELSE [Invoice Number] END AS TransID, CONVERT(date, [Invoice Date]) AS InvDate, CAST([Total Amount] AS float) AS TotalAmt,  [Customer Code], [Customer Name], CAST([Remaining Amount] AS float) AS ScalaRemAmt, LastPaymentDate AS LastScalaInvPayDate  FROM VScalaInvoice01) AS q1 INNER JOIN(SELECT TransactionID,CAST(RemainingAmount AS float) AS InVanRemAmt  FROM [Transaction]  WHERE Voided=0 AND Synchronized=1)AS q2 ON q1.TransID=q2.TransactionID COLLATE arabic_bin AND q1.ScalaRemAmt<>q2.InVanRemAmtLEFT OUTER JOIN(SELECT CusCode, LastScalaCusPayDate	FROM LastScalaCustomerPaymentDate) AS q4 ON q4.CusCode=q1.[Customer Code] ON q3.TransactionID=q1.TransID COLLATE arabic_binWHERE q1.ScalaRemAmt<>q2.InVanRemAmt  and (q4.LastScalaCusPayDate>=q3.lastInVanPayDate OR q1.LastScalaInvPayDate>=q3.lastInVanPayDate or q3.lastInVanPayDate IS NULL))                                 ";
                str = @"
update trn
set RemainingAmount=ScaInv.[Remaining Amount]
FROM

(SELECT TransactionID,CONVERT(date,MAX(PaymentDate)) AS lastInVanPayDate,OrganizationID
                           FROM   CustomerPayment
                           WHERE  PaymentStatusID<>5
                           GROUP BY TransactionID,OrganizationID
) AS q1 RIGHT OUTER JOIN

 VScalaInvoice01 as ScaInv

 INNER JOIN

  [Transaction] trn
          
 ON trn.TransactionID=(case when ScaInv.[InCube Reference] like 'INV-%' or ScaInv.[InCube Reference] like 'OrdInv-%' then [InCube Reference] else [Invoice Number] end) COLLATE arabic_bin 
 and right(ScaInv.OrgCode,1)=trn.OrganizationID AND ScaInv.[Remaining Amount]<>trn.RemainingAmount
 
LEFT OUTER JOIN

  LastScalaCustomerPaymentDate as ScaCusPayDate

 ON CusCode=ScaInv.[Customer Code] and ScaCusPayDate.[OrgID]=right(ScaInv.OrgCode,1)
 ON q1.TransactionID=(case when ScaInv.[InCube Reference] like 'INV-%' or ScaInv.[InCube Reference] like 'OrdInv-%' then [InCube Reference] else [Invoice Number] end) COLLATE arabic_bin 
 and q1.OrganizationID=right(ScaInv.OrgCode,1)

WHERE ScaInv.[Remaining Amount]<>trn.RemainingAmount and /*trn.Voided=0 and trn.Synchronized=1 and --*/
      (ScaCusPayDate.LastScalaCusPayDate>=q1.lastInVanPayDate OR ScaInv.LastPaymentDate>=q1.lastInVanPayDate or q1.lastInVanPayDate IS NULL)
";
                //  collate Arabic_BIN 
                query = new InCubeQuery(db_vms, str);
                err = query.ExecuteNonQuery();
            }
            catch (Exception ex)
            { }

        }

        #endregion

        #region UpdateFullInvoices
        public override void UpdateInvoice()
        {
            this.DefaultOrganization();
            TotalInserted = 0;
            TotalIgnored = 0;
            string Status = "";
            string DocNumber = "";
            DateTime importDate = DateTime.Now;


            InCubeErrors err;

            object field = new object();

            dbTrans = new InCubeTransaction();

            DataTable DT = new DataTable();

            string QueryString = @"SELECT
                InCubeSalesInvoiceHeaderView.TransactionID, 
                InCubeSalesInvoiceHeaderView.CustomerCode,
                InCubeSalesInvoiceHeaderView.MasterCustomerCode,
                InCubeSalesInvoiceHeaderView.TransactionType,
                InCubeSalesInvoiceHeaderView.TransactionDate,
                InCubeSalesInvoiceHeaderView.NetAmount,
                InCubeSalesInvoiceHeaderView.RemainingAmount,
                InCubeSalesInvoiceHeaderView.TaxAmount,
                InCubeSalesInvoiceHeaderView.EmployeeCode,
                InCubeSalesInvoiceHeaderView.OrganizationCode,
                InCubeSalesInvoiceHeaderView.Currency,
                InCubeSalesInvoiceHeaderView.OrderID

                FROM         [InCubeSalesInvoiceHeaderView]
                WHERE ([InCubeSalesInvoiceHeaderView].TransactionType = 1) AND NOT EXISTS(Select TransactionID FROM SalesInvoicesHistory WHERE InCubeSalesInvoiceHeaderView.TransactionID = SalesInvoicesHistory.TransactionID)";


            incubeQuery = new InCubeQuery(db_ERP, QueryString);
            incubeQuery.Execute();
            DT = incubeQuery.GetDataTable();
            if (DT == null || DT.Rows.Count == 0)
            {
                WriteMessage(" ");
                WriteMessage("No Invoices data found\r\n");
                return;
            }

            WriteMessage(" ");
            WriteMessage("<<< Updating Invoices >>>\r\n");

          ClearProgress();
            SetProgressMax(DT.Rows.Count);
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                try
                {
                    err = dbTrans.BeginTransaction(db_vms);
                      ReportProgress("Updating Invoices");
 
                    string TranType = DT.Rows[i]["TransactionType"].ToString();//TRANSACTION_TYPE
                    DocNumber = DT.Rows[i]["TransactionID"].ToString();//TRANSACTION_NUMBER
                    DateTime Date = DateTime.Parse(DT.Rows[i]["TransactionDate"].ToString());//TRANSACTION_DATE
                    string CustomerNumber = DT.Rows[i]["MasterCustomerCode"].ToString();//CUSTOMERCODE
                    string OutletNumber = DT.Rows[i]["CustomerCode"].ToString();//CUSTOMERCODE
                    string SalesID = DT.Rows[i]["EmployeeCode"].ToString();//SALESPERSON_CODE
                    string Total = DT.Rows[i]["NetAmount"].ToString();//TOTAL_AMOUNT
                    string Ramount = DT.Rows[i]["RemainingAmount"].ToString();//REMAINING_AMOUNT
                    string Tax = DT.Rows[i]["TaxAmount"].ToString();//Tax_AMOUNT
                    string InVanOrderNumber = DT.Rows[i]["OrderID"].ToString();//Order_ID
                    string Currency = DT.Rows[i]["Currency"].ToString();//Currency
                    //string VanCode = DT.Rows[i]["VanCode"].ToString();//Van_Code
                    string CompanyID = DT.Rows[i]["OrganizationCode"].ToString();//Order_ID


                    string OrgID = GetFieldValue("Organization", "OrganizationID", "OrganizationCode = '" + CompanyID + "'", db_vms, dbTrans);
                    string OutletID = GetFieldValue("CustomerOutlet", "OutletID", "CustomerOutlet.CustomerCode = '" + OutletNumber + "'", db_vms, dbTrans);
                    string CustomerID = GetFieldValue("CustomerOutlet ", "CustomerID", "CustomerOutlet.CustomerCode = '" + OutletNumber + "'", db_vms, dbTrans);

                    string AccountID = GetFieldValue("AccountCustOut", "AccountID", "CustomerID = " + CustomerID + " and OutletID=" + OutletID, db_vms, dbTrans);

                    if (CustomerID == string.Empty || OutletID == string.Empty || AccountID == string.Empty || CompanyID == string.Empty)
                    {
                        if (CustomerID == string.Empty || OutletID == string.Empty)
                        {
                            Status = "Customer ID or Outlet ID Doesn't Exist";
                            WriteMessage(" ");
                           WriteMessage("Customer ID or Outlet ID not found for this transaction : " + DocNumber + "\r\n");
                        }
                        else if (AccountID == string.Empty)
                        {
                            Status = "Account ID Doesn't Exist";
                           WriteMessage(" ");
                           WriteMessage("Account ID not found for this transaction : " + DocNumber + "\r\n");
                        }
                        else
                        {
                            Status = "Company ID Doesn't Exist";
                           WriteMessage(" ");
                           WriteMessage("Company ID not found for this transaction : " + DocNumber + "\r\n");
                        }
                        TotalIgnored++;

                        continue;
                    }
                    string SalespersonID = "0";
                    if (SalesID != "")
                    {
                        SalespersonID = GetFieldValue("Employee", "EmployeeID", "EmployeeCode ='" + SalesID + "'", db_vms, dbTrans);

                        if (SalespersonID == string.Empty)
                        {
                            Status = "Employee ID Doesn't Exist";
                            TotalIgnored++;
                           WriteMessage(" ");
                           WriteMessage("Employee ID not found for this invoice :" + DocNumber + "\r\n");
                            continue;
                        }
                    }
                    // string RouteID = GetFieldValue("EmployeeTerritory", "TerritoryID", "EmployeeID = " + SalespersonID, db_vms);

                    err = ExistObject("[Transaction]", "TransactionID", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms, dbTrans);
                    if (err != InCubeErrors.Success)
                    {
                        TotalInserted++;
                        QueryBuilderObject = new QueryBuilder();
                        QueryBuilderObject.SetField("CustomerID", CustomerID);
                        QueryBuilderObject.SetField("OutletID", OutletID);
                        QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                        QueryBuilderObject.SetField("EmployeeID", SalespersonID);
                        QueryBuilderObject.SetDateField("TransactionDate", Date);
                        QueryBuilderObject.SetField("TransactionTypeID", TranType);
                        QueryBuilderObject.SetField("Discount", "0");
                        QueryBuilderObject.SetField("Synchronized", "1");
                        QueryBuilderObject.SetField("RemainingAmount", Ramount.Replace("-", ""));
                        QueryBuilderObject.SetField("Grosstotal", Total);
                        QueryBuilderObject.SetField("Nettotal", Total);
                        QueryBuilderObject.SetField("Tax", Tax);
                        QueryBuilderObject.SetField("Posted", "1");
                        QueryBuilderObject.SetField("DivisionID", "-1");
                        QueryBuilderObject.SetField("AccountID", AccountID);
                        QueryBuilderObject.SetField("OrganizationID", OrgID);
                        QueryBuilderObject.SetField("RefOrderID", "'" + InVanOrderNumber + "'");
                        err = QueryBuilderObject.InsertQueryString("[Transaction]", db_vms, dbTrans);
                    }
                    else
                    {
                        err = ExistObject("TransactionDetail", "TransactionID", "TransactionID ='" + DocNumber + "' and CustomerID=" + CustomerID, db_vms, dbTrans);
                        if (err == InCubeErrors.Success)
                        {
                            Status = "Invoice already exist.";
                            TotalIgnored++;
                           WriteMessage(" ");
                           WriteMessage("Invoice already exist : " + DocNumber + "\r\n");
                            dbTrans.Rollback();
                            continue;
                        }
                        else
                        {
                            Status = "Invoice header already exist but details was missing.";
                            err = InCubeErrors.Success;
                        }
                    }

                    #region (Fill Details)
                    if (err == InCubeErrors.Success)
                    {
                        string QueryString2 = string.Format(@"SELECT     
                            InCubeSalesInvoiceDetailsView.ItemCode,
                            InCubeSalesInvoiceDetailsView.ItemType,
                            SUM(InCubeSalesInvoiceDetailsView.Quantity) AS Quantity,
                            InCubeSalesInvoiceDetailsView.UnitPrice ,
                            SUM(InCubeSalesInvoiceDetailsView.TaxValue) AS TaxValue,
                            InCubeSalesInvoiceDetailsView.UOM,
                            InCubeSalesInvoiceDetailsView.DiscountValue

                            FROM         InCubeSalesInvoiceDetailsView
                            WHERE (InCubeSalesInvoiceDetailsView.TransactionID = '{0}')
                            GROUP BY ItemCode,ItemType,UnitPrice,UOM,DiscountValue", DocNumber);

                        incubeQuery = new InCubeQuery(db_ERP, QueryString2);
                        incubeQuery.Execute();
                        DataTable DTD = new DataTable();
                        DTD = incubeQuery.GetDataTable();
                        if (DTD == null || DTD.Rows.Count == 0)
                        {
                            dbTrans.Rollback();
                           WriteMessage(" ");
                           WriteMessage("Details for this invoice not found : " + DocNumber + "\r\n");
                            TotalIgnored++;
                            continue;
                        }

                        for (int x = 0; x < DTD.Rows.Count; x++)
                        {
                            string ItemCode = DTD.Rows[x]["ItemCode"].ToString();//ItemCode
                            string SalesTransactionType = DTD.Rows[x]["ItemType"].ToString();//SalesTransactionType
                            string UOM = DTD.Rows[x]["UOM"].ToString();//UOM
                            string Quantity = DTD.Rows[x]["Quantity"].ToString();//Quantity
                            string UnitPrice = DTD.Rows[x]["UnitPrice"].ToString();//UnitPrice
                            string TaxD = DTD.Rows[x]["TaxValue"].ToString();//Tax_AMOUNT
                            string Discount = DTD.Rows[x]["DiscountValue"].ToString();//DiscountValue

                            if (TaxD.Equals(""))
                            {
                                TaxD = "0";
                            }
                            DateTime _defaultExpiryDate = new DateTime(1990, 1, 1);
                            string _defaultBatchNo = "1990/01/01";

                            string PackID = GetFieldValue("PACK INNER JOIN ITEM ON ITEM.ITEMID = PACK.ITEMID INNER JOIN PackTypeLanguage ON PackTypeLanguage.PackTypeID = PACK.PackTypeID AND LanguageID = 1", "PackID", "ITEM.ItemCode = '" + ItemCode + "'" + " AND PackTypeLanguage.Description = '" + UOM + "'", db_vms, dbTrans);

                            QueryBuilderObject = new QueryBuilder();
                            QueryBuilderObject.SetField("CustomerID", CustomerID);
                            QueryBuilderObject.SetField("OutletID", OutletID);
                            QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                            QueryBuilderObject.SetField("Tax", TaxD);
                            QueryBuilderObject.SetField("Discount", Discount);
                            QueryBuilderObject.SetField("PackID", PackID);
                            QueryBuilderObject.SetField("Quantity", Quantity);
                            QueryBuilderObject.SetField("SalesTransactionTypeID", SalesTransactionType);
                            QueryBuilderObject.SetField("Price", UnitPrice);
                            QueryBuilderObject.SetField("DivisionID", "-1");
                            QueryBuilderObject.SetField("PackStatusID", "3");
                            QueryBuilderObject.SetField("Warehoused", "0");
                            QueryBuilderObject.SetDateField("ExpiryDate", _defaultExpiryDate);
                            QueryBuilderObject.SetField("BatchNo", _defaultBatchNo);
                            QueryBuilderObject.SetField("ReturnReason", "-1");
                            QueryBuilderObject.SetField("PromotionDetailID", "-1");
                            err = QueryBuilderObject.InsertQueryString("TransactionDetail", db_vms, dbTrans);
                            if (err != InCubeErrors.Success)
                                break;
                        }
                        if (err == InCubeErrors.Success)
                        {
                            dbTrans.Commit();
                            Status = "Success.";
                        }
                        else
                        {
                            dbTrans.Rollback();
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, err.ToString()+  " ---- "+QueryBuilderObject.CurrentException.ToString()  , LoggingType.Error, LoggingFiles.InCubeLog);
              
                            Status = "Deails couldn't be inserted for this invoice";
                            TotalIgnored++;
                           WriteMessage(" ");
                           WriteMessage("Deails couldn't be inserted for this invoice : " + DocNumber + "\r\n");
                        }

                        continue;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "  " + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
                }
                finally
                {
                    QueryBuilderObject = new QueryBuilder();
                    QueryBuilderObject.SetField("TransactionID", "'" + DocNumber + "'");
                    QueryBuilderObject.SetDateField("ImportDate", importDate);
                    QueryBuilderObject.SetField("Status", "'" + Status + "'");
                    err = QueryBuilderObject.InsertQueryString("SalesInvoicesHistory", db_ERP);
                }
            }
            if (TotalInserted > 0)
            {
               WriteMessage("\r\n");
               WriteMessage("<<< INVOICES >>> (Total Inserted = " + TotalInserted + ") " + "(Total Ignored = " + TotalIgnored + ")");
                dbTrans = null;
            }
        }
        #endregion

        #endregion

        # region Build XML File

        # region Sales Order Export
        private StringBuilder BuildSalesOrderHeader(ref StringBuilder builder, string IscalaInvoiceNO, string CustCodeInv, string CustCode, string CustomerOutletCode, string OrdDate, string SalesmanNum, string WhID, string InVanInvoiceNo,string typeID,string LPO,string SalesMode,string TermPay,string ReturnReason)
        {
            try
            {
                SalesmanNum = SalesmanNum.Replace('Q', ' ').Trim();
                WhID = WhID.Replace('Q', ' ').Trim();
                if (typeID.Equals("2") || typeID.Equals("4"))
                {
                    typeID = "8";
                }
                else if (typeID.Equals("0"))
                {
                    typeID = "1";
                }
                else
                {
                    typeID = "2";
                }
                if (SalesMode.Equals("1"))
                {
                    SalesMode = "2";
                }
                else if (SalesMode.Equals("2")) SalesMode = "1";
                else
                {
                    if (TermPay.Equals("01"))
                        SalesMode = "2"; 
                    else
                        
                        SalesMode = "1"; 
                }
                if (ReturnReason.Equals("-1"))
                {
                    ReturnReason = "0"; 
                }

                // Add the Header Static
                builder.Append(@"<msg:Msg xsi:schemaLocation=$http://Epicor.com/Message/2.0 http://scshost/schemas/epicor/ScalaMessage.xsd$ xmlns:xsi=$http://www.w3.org/2001/XMLSchema-instance$ xmlns:msg=$http://Epicor.com/Message/2.0$>                        <msg:Hdr>                        <msg:Sender>");
                builder.Append(@"<msg:Name>SinioraSO</msg:Name>");
                builder.Append(@"<msg:Subname>ADD</msg:Subname>                        </msg:Sender>                        </msg:Hdr>                        <msg:Body>");
                builder.Append(@"<msg:Req msg-type=$SinioraSO$ action=$ADD$>");

                builder.Append(@"<msg:Dta>                        <dta:SalesOrder xsi:schemaLocation=$http://www.scala.net/SalesOrder/1.1 http://scshost/schemas/Scala/1.1/SalesOrder.xsd$ xmlns:msg=$http://Epicor.com/InternalMessage/1.1$ xmlns:dta=$http://www.scala.net/SalesOrder/1.1$>                        <dta:OrderHeader>");
                // Add the Order Header Dynamic
                IscalaInvoiceNO = IscalaInvoiceNO.Replace("Q", "").Trim();
                builder.Append(@"<dta:OrdNum>" + IscalaInvoiceNO + @"</dta:OrdNum>                            <dta:OrdType>1</dta:OrdType>                            <dta:CustCodeInv>" + CustCodeInv + @"</dta:CustCodeInv>                            <dta:CustCodeDeliv>" + CustCode + @"</dta:CustCodeDeliv>                            <dta:OrdDate>" + OrdDate + @"</dta:OrdDate>                            <dta:CustCodeOrd>" + CustCode + @"</dta:CustCodeOrd>                            <dta:CustPONum>" + LPO + @"</dta:CustPONum>                            <dta:QuoteDate>1900-01-01</dta:QuoteDate>                            <dta:OrdDisc>0.00</dta:OrdDisc>                            <dta:OrdStatus>" + typeID + @"</dta:OrdStatus>                            <dta:SalesmanNum>" + SalesmanNum + @"</dta:SalesmanNum>                            <dta:WhID>" + WhID + @"</dta:WhID>                            <dta:ShipMark>" + InVanInvoiceNo + @"</dta:ShipMark>                            <dta:Remark1>"+"0"+ SalesMode + @"</dta:Remark1>                             <dta:TermPay>" + TermPay + @"</dta:TermPay>");
                builder.Append(@"<dta:SalesReturnInfo>                            <dta:ReasonCode>" + ReturnReason + @"</dta:ReasonCode>                            </dta:SalesReturnInfo>                             </dta:OrderHeader>                                <dta:OrderLines>");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildSalesOrderLine(ref StringBuilder builder, string LineNumber, string ItemCode, string Quantity, string UnitPrice, string DeliveryDate, string Discount)
        {
            try
            {
                ItemCode = ItemCode.Replace('Q', ' ').Trim();
                // Add the Order Line Dynamic
                builder.Append(@"<dta:OrderLine>
        <dta:LineNum>" + LineNumber + @"</dta:LineNum>
        <dta:StockCode>" + ItemCode + @"</dta:StockCode>
        <dta:QtyOrdered>" + Quantity + @"</dta:QtyOrdered>
        <dta:UnitPrice>" + UnitPrice + @"</dta:UnitPrice>
        <dta:DelivDateRequest>" + DeliveryDate + @"</dta:DelivDateRequest>
        <dta:CustDisc>" + Discount + @"</dta:CustDisc>
        <dta:DescrLine2></dta:DescrLine2>
        <dta:SellPriceMult>1</dta:SellPriceMult>
        </dta:OrderLine>
");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildSalesOrderFooter(ref StringBuilder builder)
        {
            try
            {
                builder.Append(@"</dta:OrderLines>                            </dta:SalesOrder>                            </msg:Dta>                            </msg:Req>                            </msg:Body>                            </msg:Msg>");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        # endregion

        # region Payments Export
        private StringBuilder BuildPayments(ref StringBuilder builder, string IscalaInvoiceNO, string CustCode, string PaymentDate, string PaymentAmount, string InVanInvoiceNo,string VanCode ,string Accountid )
        {
            try
            {
                // Add the Header Static
                builder.Append(@"<msg:Msg xsi:schemaLocation=$http://Epicor.com/Message/2.0 http://scshost/schemas/epicor/ScalaMessage.xsd$ xmlns:xsi=$http://www.w3.org/2001/XMLSchema-instance$ xmlns:msg=$http://Epicor.com/Message/2.0$>
<msg:Hdr>
<msg:Sender>
<msg:Name>SinioraPayment</msg:Name>
<msg:Subname>ADD</msg:Subname>
</msg:Sender>
</msg:Hdr>
<msg:Body>
<msg:Req msg-type=$SinioraPayment$ action=$ADD$>
<msg:Dta>
<dta:CustomerPayment xsi:schemaLocation=$http://www.scala.net/CustomerPayment/1.1 http://scshost/Schemas/Scala/1.1/CustomerPayment.xsd$ xmlns:dta=$http://www.scala.net/CustomerPayment/1.1$>
<dta:CustCode>" + CustCode + @"</dta:CustCode>
<dta:InvNum>" + IscalaInvoiceNO + @"</dta:InvNum>
<dta:PaymDate>" + PaymentDate + @"</dta:PaymDate>
<dta:PaidAmntLCU>" + PaymentAmount + @"</dta:PaidAmntLCU>
<dta:PaidAmntOCU>" + PaymentAmount + @"</dta:PaidAmntOCU>
<dta:CurrCodeOCU>ILS</dta:CurrCodeOCU>
<dta:CurrCodeLCU>ILS</dta:CurrCodeLCU>
<dta:DiscAmntOCU>0</dta:DiscAmntOCU>
<dta:TransText>" + InVanInvoiceNo + @"</dta:TransText>
<dta:AccStr>
<dta:Account>"+Accountid+@"</dta:Account>
<dta:AccDim5>" + VanCode + @"</dta:AccDim5>
</dta:AccStr>
</dta:CustomerPayment>
</msg:Dta>
</msg:Req>
</msg:Body>
</msg:Msg>
");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        # endregion

        # region Transfers Export
        private StringBuilder BuildTransfersHeader(ref StringBuilder builder, string IscalaInvoiceNO, string ToWh, string FromWh, string TransDate)
        {
            try
            {
                // Add the Header Static
                builder.Append(@"<msg:Msg xsi:schemaLocation=$http://Epicor.com/Message/2.0 http://scshost/schemas/epicor/ScalaMessage.xsd$ xmlns:xsi=$http://www.w3.org/2001/XMLSchema-instance$ xmlns:msg=$http://Epicor.com/Message/2.0$>
<msg:Hdr>
<msg:Sender>
<msg:Name>SinioraReq</msg:Name>
<msg:Subname>update</msg:Subname>
</msg:Sender>
</msg:Hdr>
<msg:Body>
<msg:Req msg-type=$SinioraReq$ action=$update$>
<msg:Dta>
<dta:Requisition xsi:schemaLocation=$http://www.scala.net/Requisition/1.1 http://scshost/schemas/Scala/1.1/Requisition.xsd$ action=$Update$ xmlns:msg=$http://Epicor.com/InternalMessage/1.1$ xmlns:dta=$http://www.scala.net/Requisition/1.1$>
<dta:RqsnNum>" + IscalaInvoiceNO + @"</dta:RqsnNum>
<dta:TemplateDescr></dta:TemplateDescr>
<dta:DeptCode>" + ToWh + @"</dta:DeptCode>
<dta:RqsnDate>" + TransDate + @"</dta:RqsnDate>
<dta:RqsnType>2</dta:RqsnType>
<dta:DelivDateTime>" + TransDate + @"</dta:DelivDateTime>
<dta:WhCode>" + FromWh + @"</dta:WhCode>
<dta:RqsnLines>
");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildTransfersLine(ref StringBuilder builder,string LineNumber, string ToWh, string FromWh, string ItemCode,string UOM,string Qty)
        {
            try
            {
                ItemCode = ItemCode.Replace('Q', ' ').Trim();
                // Add the Order Line Dynamic
                builder.Append(@"<dta:RqsnLine action=$Update$>
        <dta:LineNum>" + LineNumber + @"</dta:LineNum>
        <dta:WhCode>" + FromWh + @"</dta:WhCode>
        <dta:StockCode registeredItem=$1$ >" + ItemCode + @"</dta:StockCode>
        <dta:StockDescr></dta:StockDescr>
        <dta:StockDescr2></dta:StockDescr2>
        <dta:QtyOrdered unitName=$" + UOM + @"$>" + Qty + @"</dta:QtyOrdered>
        </dta:RqsnLine>
");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildTransfersFooter(ref StringBuilder builder)
        {
            try
            {
                builder.Append(@"</dta:RqsnLines>                            </dta:Requisition>                            </msg:Dta>                            </msg:Req>                            </msg:Body>                            </msg:Msg>");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        # endregion

        # region Stock Withdrawal Export
        private StringBuilder BuildStockWithdrawalHeader(ref StringBuilder builder, string OrderNo, string CustCode, string CustomerOutletCode, string OrdDate, string SalesmanNum, string WhID, string InVanInvoiceNo)
        {
            try
            {
                SalesmanNum = SalesmanNum.Replace('Q', ' ').Trim();
                WhID = WhID.Replace('Q', ' ').Trim();

                // Add the Header Static
                builder.Append(@"<msg:Msg xsi:schemaLocation=$http://Epicor.com/Message/2.0 http://scshost/schemas/epicor/ScalaMessage.xsd$ xmlns:xsi=$http://www.w3.org/2001/XMLSchema-instance$ xmlns:msg=$http://Epicor.com/Message/2.0$>	                            <msg:Hdr>		                            <msg:Sender>");
                builder.Append(@"<msg:Name>SinioraDel</msg:Name>");
                builder.Append(@"        <msg:Subname>update</msg:Subname>		                            </msg:Sender>	                            </msg:Hdr>	                            <msg:Body>");
                builder.Append(@"<msg:Req msg-type=$SinioraDel$ action=$update$>");
                OrderNo = OrderNo.Replace("Q", "").Trim();
                builder.Append(@"   <msg:Dta>				                            <dta:SalesOrderDelivery xsi:schemaLocation=$http://www.scala.net/SalesOrderDelivery/1.1 http://scshost/schemas/Scala/1.1/SalesOrderDelivery.xsd$ xmlns:msg=$http://Epicor.com/InternalMessage/1.1$ xmlns:dta=$http://www.scala.net/SalesOrderDelivery/1.1$>                            <dta:OrderHeader>                            <dta:OrdNum>" + OrderNo + @"</dta:OrdNum>                            </dta:OrderHeader>                            <dta:OrderLineList>");

            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildStockWithdrawalLine(ref StringBuilder builder, string LineNumber, string ItemCode, string Quantity, string DeliveryDate)
        {
            try
            {

                ItemCode = ItemCode.Replace('Q', ' ').Trim();
                // Add the Order Line Dynamic
                builder.Append(@"<dta:OrderLine>
        <dta:LineNum>" + LineNumber + @"</dta:LineNum>
        <dta:StockCode>" + ItemCode + @"</dta:StockCode>
        <dta:QtyDeliv>" + Quantity + @"</dta:QtyDeliv>
        <dta:DelivDateAct>" + DeliveryDate + @"</dta:DelivDateAct>
        </dta:OrderLine>");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        private StringBuilder BuildStockWithdrawalFooter(ref StringBuilder builder)
        {
            try
            {
                builder.Append(@"</dta:OrderLineList>				                </dta:SalesOrderDelivery>			                </msg:Dta>		                </msg:Req>	                </msg:Body>                </msg:Msg>");
            }
            catch (Exception ex)
            {
                builder = null;
            }
            return builder;

        }
        # endregion

        # endregion

        #region Send Data

        #region SendInvoices
        public override void SendInvoices()
        {

            object TranID = "";
            object TranDate = "";
            object Store = "";
            object Customer = "";
            object Employee = "";
            object Total = "";
            object Gross = "";
            object ItemCode = "";
            object Value = "";
            object ReturnReason = "";
            object Quantity = "";
            object Price = "";
            object Batch = "";
            object TaxPercentage = "";
            object Tax = "";
            object Discount = "";
            object Remaining = "";
            object Outlet = "";
            object PackID = "";
            object SalesMode = "";
            object WHCode = "";
            object OrgID = "";
            object TypeID = "";
            object LPO = "";
            object TermPay = "";
            object PackStatus = "";
            object TransactionType = "";
            object CustomerType = "";
            object EmployeeType = "";
            object SourceTransaction="";
            string EmpID;


           WriteMessage("\r\n" + "Sending Invoices");

            

        string    QueryString = @"SELECT  [Transaction].TransactionID, Warehouse.WarehouseCode, 
Employee.EmployeeCode, [Transaction].TransactionDate, CustomerOutlet.CustomerCode, [Transaction].NetTotal,
[Transaction].GrossTotal, [Transaction].RemainingAmount, CustomerOutlet.OutletID,[Transaction].SalesMode,
'FG01' WarehouseCode,[Transaction].Organizationid,[Transaction].TransactionTypeID,[Transaction].LPONumber, 
case ISNULL(PaymentTermLanguage.Description,'0') when '0' then '01-Cash Payment' else PaymentTermLanguage.Description end 'Description',
(SELECT TOP 1 TransactionDetail.ReturnReason FROM TransactionDetail WHERE TransactionDetail.TransactionID = [Transaction].TransactionID) AS ReturnReason ,
isNull((SELECT TOP 1 TransactionDetail.PackStatusID FROM TransactionDetail WHERE TransactionDetail.TransactionID = [Transaction].TransactionID),0) As PackStatusID , 
 [Transaction].TransactionTypeID , customeroutlet.CustomerTypeID ,Employee.EmployeeTypeID ,[transaction].SourceTransactionID
FROM [Transaction]
INNER JOIN Employee ON [Transaction].EmployeeID = Employee.EmployeeID 
INNER JOIN EmployeeVehicle ON [Transaction].EmployeeID = EmployeeVehicle.EmployeeID
INNER JOIN Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID
INNER JOIN CustomerOutlet ON [Transaction].CustomerID = CustomerOutlet.CustomerID AND [Transaction].OutletID = CustomerOutlet.OutletID
INNER JOIN Customer ON CustomerOutlet.CustomerID = Customer.CustomerID
--INNER JOIN VehicleLoadingWh ON Warehouse.WarehouseID=VehicleLoadingWh.VehicleID
--INNER JOIN Warehouse MW ON VehicleLoadingWh.WarehouseID = MW.WarehouseID
LEFT JOIN PaymentTermLanguage ON CustomerOutlet.PaymentTermID = PaymentTermLanguage.PaymentTermID AND LanguageID = 1
left join transactionhistory th on th.transactionid = [Transaction].TransactionID
WHERE      (([Transaction].Synchronized = 0) and (th.transactionid is null)) AND ([Transaction].Voided = 0) AND ([Transaction].TransactionTypeID in (1,2,3,4)) 
AND (Customer.New = 0) AND ( [Transaction].TransactionDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + @"' 
AND   [Transaction].TransactionDate < '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
"; 

            if (Filters.EmployeeID!=-1)
            {
                QueryString += " AND [Transaction].EmployeeID = " + Filters.EmployeeID;
            }
            QueryString += " ORDER BY [Transaction].TransactionDate, [Transaction].TransactionID";
            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);
            err = GetSalesTransactionInformation.Execute();

            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                int Counter = 0;
                int Counter2 = 0;


                try
                {

                    #region Get SalesTransaction Information
                    {
                        err = GetSalesTransactionInformation.GetField(0, ref TranID);
                        err = GetSalesTransactionInformation.GetField(1, ref  Store);
                        err = GetSalesTransactionInformation.GetField(2, ref  Employee);
                        err = GetSalesTransactionInformation.GetField(3, ref TranDate);
                        err = GetSalesTransactionInformation.GetField(4, ref Customer);
                        err = GetSalesTransactionInformation.GetField(5, ref Total);
                        err = GetSalesTransactionInformation.GetField(6, ref Gross);
                        err = GetSalesTransactionInformation.GetField(7, ref Remaining);
                        err = GetSalesTransactionInformation.GetField(8, ref Outlet);
                        err = GetSalesTransactionInformation.GetField(9, ref SalesMode);
                        err = GetSalesTransactionInformation.GetField(10, ref WHCode);
                        err = GetSalesTransactionInformation.GetField(11, ref OrgID);
                        err = GetSalesTransactionInformation.GetField(12, ref TypeID);
                        err = GetSalesTransactionInformation.GetField(13, ref LPO);
                        err = GetSalesTransactionInformation.GetField(14, ref TermPay);
                        err = GetSalesTransactionInformation.GetField(15, ref ReturnReason);
                        err = GetSalesTransactionInformation.GetField(16, ref PackStatus);
                        err = GetSalesTransactionInformation.GetField(17, ref TransactionType);
                        err = GetSalesTransactionInformation.GetField(18, ref CustomerType);
                        err = GetSalesTransactionInformation.GetField(19, ref EmployeeType);
                        err = GetSalesTransactionInformation.GetField(20,ref SourceTransaction);



                    }
                    #endregion
                    TermPay = GetFieldValue("invan_payment_term", "Payment_Term_ID", "Payment_Term_ID +'-'+Ltrim(Rtrim(PaymentDesc)) ='" + TermPay.ToString().Trim() + "'", db_ERP);
                    string IscalaMaximumSalesOrderNumber = TranID.ToString();
                    
                    if ((EmployeeType.ToString() == "3") && (SourceTransaction.ToString() != string.Empty) )
                    {
                        EmpID = GetFieldValue("SalesOrder", "employeeid", "Ltrim(Rtrim(Orderid)) ='" + SourceTransaction.ToString().Trim() + "'", db_vms);
                        Employee = GetFieldValue("Employee", "EmployeeCode", "Employeeid = " + EmpID, db_vms);
                    }

                    StringBuilder SalesOrderString = new StringBuilder();
                    StringBuilder StockWithdrwalString = new StringBuilder();

                    DateTime dtDate = Convert.ToDateTime(TranDate);

                    string CustCodeINV = "";
                    CustCodeINV = Customer.ToString();
                    string WhId = Store.ToString(); // Van Code
                    if (TransactionType.ToString() == "3" || TransactionType.ToString() == "4")
                    {
                        SalesMode = "2";
                        if (CustomerType.ToString() == "1")
                        {
                            TermPay = "21"; 
                        }

                    }
                    BuildSalesOrderHeader(ref SalesOrderString, IscalaMaximumSalesOrderNumber.ToString(), CustCodeINV.ToString(), Customer.ToString(), Outlet.ToString(), dtDate.ToString("yyyy-MM-dd"), Employee.ToString(), WhId, TranID.ToString(), TypeID.ToString(), LPO.ToString(), SalesMode.ToString(), TermPay.ToString(), ReturnReason.ToString());
                    // Call for items withdrawal
                    BuildStockWithdrawalHeader(ref StockWithdrwalString, IscalaMaximumSalesOrderNumber, Customer.ToString(), Outlet.ToString(), dtDate.ToString("yyyy-MM-dd"), Employee.ToString(), WhId, TranID.ToString());


                    string dtlQryStr = @"SELECT  
                          TRANSACTIONDETAIL.QUANTITY , 
                          TRANSACTIONDETAIL.PRICE , 
                          TRANSACTIONDETAIL.DISCOUNT , 
                          TRANSACTIONDETAIL.TAX , 
                          ITEM.PACKDEFINITION  AS ITEMCODE, 
                          TRANSACTIONDETAIL.PACKID, 
                          cast((TRANSACTIONDETAIL.PRICE) + ((TRANSACTIONDETAIL.TAX) /  TRANSACTIONDETAIL.QUANTITY) as numeric(19,2))AS VALUE 
                          FROM   TRANSACTIONDETAIL 
                          INNER JOIN PACK ON TRANSACTIONDETAIL.PACKID = PACK.PACKID 
                          INNER JOIN ITEM ON PACK.ITEMID = ITEM.ITEMID 
                          WHERE  ( TRANSACTIONDETAIL.TRANSACTIONID = '" + TranID.ToString() + @"' )";


                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    err = dtlQry.FindFirst();

                    if (err != InCubeErrors.Success)
                    {
                        throw new Exception("No details found");
                    }

                    while (err == InCubeErrors.Success)
                    {
                        object SalesTransactionType = 0;
                        
                        #region Get SalesTransaction Information
                        {
                            err = dtlQry.GetField(0, ref Quantity);
                            err = dtlQry.GetField(1, ref Price);
                            err = dtlQry.GetField(2, ref Discount);
                            err = dtlQry.GetField(3, ref Tax);
                            err = dtlQry.GetField(4, ref ItemCode);
                            err = dtlQry.GetField(5, ref PackID);
                            err = dtlQry.GetField(6, ref Value);
                        }
                        #endregion

                        decimal DiscountValue = 0;
                        decimal unitPrice = 0;
                        decimal taxvalue = 0;

                        if (Discount.ToString() != "")
                        { 
                            DiscountValue = decimal.Parse(Discount.ToString());
                            if ((Price.ToString() != "") && DiscountValue > 0 && decimal.Parse(Price.ToString()) != 0)
                            {
                                DiscountValue = (DiscountValue * 100) / (decimal.Parse(Price.ToString()) * decimal.Parse(Quantity.ToString())); //discount per line converted to percentage
                            }
                        }
                        DiscountValue = Math.Truncate(decimal.Parse(( decimal.Parse(100.0.ToString()) * DiscountValue).ToString())) / 100;

                        if (Price.ToString() != "" && decimal.Parse(Price.ToString()) != 0  )
                        {
                            if (Discount.ToString() != "")
                            {
                                unitPrice = decimal.Parse(Price.ToString()) + decimal.Parse(Price.ToString())*(decimal.Parse(Tax.ToString()) / ((decimal.Parse(Price.ToString()) * decimal.Parse(Quantity.ToString())) - decimal.Parse(Discount.ToString())));
                            }
                            else
                            {
                                unitPrice = decimal.Parse(Price.ToString()) + (decimal.Parse(Tax.ToString()) / decimal.Parse(Quantity.ToString()));
                            }
                        }
                        unitPrice = decimal.Parse( unitPrice.ToString("F2")) ;
                        
                        if (Tax.ToString() != "")
                        { taxvalue = decimal.Parse(Tax.ToString()); }

                        dtDate = Convert.ToDateTime(TranDate);
                        Counter++;
                        Counter2++;


                        if ((TypeID.ToString() == "2") || (TypeID.ToString() == "4"))
                        {
                            BuildSalesOrderLine(ref SalesOrderString, Counter.ToString(), ItemCode.ToString(), "-"+Quantity.ToString(), unitPrice.ToString(), dtDate.ToString("yyyy-MM-dd"), DiscountValue.ToString());
                            BuildStockWithdrawalLine(ref StockWithdrwalString, Counter2.ToString(), ItemCode.ToString(), "-"+Quantity.ToString(), dtDate.ToString("yyyy-MM-dd"));
                        }
                        else
                        {
                            BuildSalesOrderLine(ref SalesOrderString, Counter.ToString(), ItemCode.ToString(), Quantity.ToString(), unitPrice.ToString(), dtDate.ToString("yyyy-MM-dd"), DiscountValue.ToString());
                            BuildStockWithdrawalLine(ref StockWithdrwalString, Counter2.ToString(), ItemCode.ToString(), Quantity.ToString(), dtDate.ToString("yyyy-MM-dd"));
                        }

                        err = dtlQry.FindNext();
                    }
                    BuildSalesOrderFooter(ref SalesOrderString);
                    // Call for items withdrawal
                    BuildStockWithdrawalFooter(ref StockWithdrwalString);

                    SalesOrderString = SalesOrderString.Replace('$', '"');
                    StockWithdrwalString = StockWithdrwalString.Replace('$', '"');

                    string sFilePath = "";

                    string sFilePathWithdrawal = "";

                    sFilePath = FilePathSalesAndReturn;
                    sFilePathWithdrawal = FilePathSalesAndReturnWithdrwal;

                    string SalesFileName = TranID.ToString() + ".XML";
                    string WithdrawalFileName = TranID.ToString() + "-Withdrawal.XML";

                    System.IO.File.WriteAllText(sFilePath + SalesFileName, SalesOrderString.ToString());
                    System.IO.File.WriteAllText(sFilePath + "\\Backup\\" + SalesFileName, SalesOrderString.ToString());
                    System.IO.File.WriteAllText(sFilePathWithdrawal + "\\" + WithdrawalFileName, StockWithdrwalString.ToString());
                    System.IO.File.WriteAllText("Z:\\Withdrawal\\Backup Withdrawal\\" + WithdrawalFileName, StockWithdrwalString.ToString());


                    QueryBuilderObject.SetField("Synchronized", "1");
                    QueryBuilderObject.UpdateQueryString("[Transaction]", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                    QueryBuilderObject.SetField("transactionid", "'"+TranID+"'");
                    QueryBuilderObject.SetField("customercode", "'" + Customer + "'");
                    QueryBuilderObject.InsertQueryString("transactionhistory", db_vms);


                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                   WriteMessage("\r\n" + TranID.ToString() + " - FAILED!");
                }

                err = GetSalesTransactionInformation.FindNext();
            }
        }
        #endregion

        #region SendOrders
        public override void SendOrders()
        {

            object TranID = "";
            object TranDate = "";
            object Store = "";
            object Customer = "";
            object Employee = "";
            object Total = "";
            object Gross = "";
            object ItemCode = "";
            object Value = "";
            object ReturnReason = "";
            object Quantity = "";
            object Price = "";
            object Batch = "";
            object TaxPercentage = "";
            object Tax = "";
            object Discount = "";
            object Remaining = "";
            object Outlet = "";
            object PackID = "";
            object SalesMode = "";
            object WHCode = "";
            object OrgID = "";
            object TypeID = "";
            object LPO = "";
            object TermPay = "";
            

           WriteMessage("\r\n" + "Sending Invoices");


            string  QueryString = @" SELECT  SalesOrder.OrderID, Warehouse.WarehouseCode, 
                Employee.EmployeeCode, SalesOrder.OrderDate, CustomerOutlet.CustomerCode, SalesOrder.NetTotal,
                SalesOrder.GrossTotal,CustomerOutlet.OutletID,
                MW.WarehouseCode,SalesOrder.Organizationid,SalesOrder.LPO, 
                case ISNULL(PaymentTermLanguage.Description,'0') when '0' then '01-Cash Payment' else PaymentTermLanguage.Description end 'Description',
                (SELECT TOP 1 SalesOrderDetail.ReturnReason FROM SalesOrderDetail WHERE SalesOrderDetail.OrderID = SalesOrder.OrderID) AS ReturnReason
                FROM SalesOrder
                INNER JOIN Employee ON SalesOrder.EmployeeID = Employee.EmployeeID 
                INNER JOIN EmployeeVehicle ON SalesOrder.EmployeeID = EmployeeVehicle.EmployeeID
                INNER JOIN Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID
                INNER JOIN CustomerOutlet ON SalesOrder.CustomerID = CustomerOutlet.CustomerID AND SalesOrder.OutletID = CustomerOutlet.OutletID
                INNER JOIN Customer ON CustomerOutlet.CustomerID = Customer.CustomerID
                INNER JOIN VehicleLoadingWh ON Warehouse.WarehouseID = VehicleLoadingWh.VehicleID
                INNER JOIN Warehouse MW ON VehicleLoadingWh.WarehouseID = MW.WarehouseID
                LEFT JOIN PaymentTermLanguage ON CustomerOutlet.PaymentTermID = PaymentTermLanguage.PaymentTermID AND LanguageID = 1
                WHERE     (SalesOrder.Synchronized = 0)AND (Employee.Mobile <> '1111' or employee.Mobile is null)
                AND (Customer.New = 0) AND ( SalesOrder.OrderDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") + @"' 
                AND   SalesOrder.OrderDate < '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')
             ";
            if (Filters.EmployeeID != -1)
            {
                QueryString += " AND SalesOrder.EmployeeID = " + Filters.EmployeeID;
            }



            QueryString += " ORDER BY SalesOrder.OrderDate, SalesOrder.OrderID";
            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);
            err = GetSalesTransactionInformation.Execute();

            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                int Counter = 0;
                int Counter2 = 0;

               
                try
                {

                    #region Get SalesTransaction Information
                    {
                        err = GetSalesTransactionInformation.GetField(0, ref TranID);
                        err = GetSalesTransactionInformation.GetField(1, ref  Store);
                        err = GetSalesTransactionInformation.GetField(2, ref  Employee);
                        err = GetSalesTransactionInformation.GetField(3, ref TranDate);
                        err = GetSalesTransactionInformation.GetField(4, ref Customer);
                        err = GetSalesTransactionInformation.GetField(5, ref Total);
                        err = GetSalesTransactionInformation.GetField(6, ref Gross);
                        err = GetSalesTransactionInformation.GetField(7, ref Outlet);
                        err = GetSalesTransactionInformation.GetField(8, ref WHCode);
                        err = GetSalesTransactionInformation.GetField(9, ref OrgID);
                        err = GetSalesTransactionInformation.GetField(10, ref LPO);
                        err = GetSalesTransactionInformation.GetField(11, ref TermPay);
                        err = GetSalesTransactionInformation.GetField(12, ref ReturnReason);
                    }
                    #endregion
                    TermPay = GetFieldValue("invan_payment_term", "Payment_Term_ID", "Payment_Term_ID +'-'+Ltrim(Rtrim(PaymentDesc)) ='" + TermPay.ToString().Trim() + "'", db_ERP);
                    string IscalaMaximumSalesOrderNumber = TranID.ToString();

                    StringBuilder SalesOrderString = new StringBuilder();
                    StringBuilder StockWithdrwalString = new StringBuilder();

                    //Outlet = GetFieldValue("Organization", "OrganizationID", "OrganizationID = 1", db_vms);
                    DateTime dtDate = Convert.ToDateTime(TranDate);

                    string CustCodeINV = "";

                    CustCodeINV = Customer.ToString();

                    string QueryString1 = @"select distinct ItemCategoryCode from SalesOrderDetail sod inner join pack p
on sod.PackID = p.PackID 
inner join item i on i.ItemID = p.ItemID
inner join ItemCategory ic on ic.ItemCategoryID = i.ItemCategoryID
where ic.ItemCategoryCode = '12' and OrderID ='" + TranID.ToString() + @"'";
                    InCubeQuery QryString1 = new InCubeQuery(db_vms, QueryString1);
                    err = QryString1.Execute();
                    err = QryString1.FindFirst();
                    string WhId;
                    if (err != InCubeErrors.Success)
                    {
                        WhId = "FG01";
                    }
                    else
                    {
                        WhId = "FG02";
                    }

                  
                     // Van Code

                    BuildSalesOrderHeader(ref SalesOrderString, IscalaMaximumSalesOrderNumber.ToString(), CustCodeINV.ToString(), Customer.ToString(), Outlet.ToString(), dtDate.ToString("yyyy-MM-dd"), Employee.ToString(), WhId, TranID.ToString(), "0", LPO.ToString(), "", TermPay.ToString(), ReturnReason.ToString());

                   // Call for items withdrawal
                   // BuildStockWithdrawalHeader(ref StockWithdrwalString, IscalaMaximumSalesOrderNumber, Customer.ToString(), Outlet.ToString(), dtDate.ToString("yyyy-MM-dd"), Employee.ToString(), WHCode.ToString(), TranID.ToString());

                    string dtlQryStr = @"SELECT  
                                      SALESORDERDETAIL.QUANTITY, 
                                      SALESORDERDETAIL.PRICE , 
                                      SALESORDERDETAIL.DISCOUNT, 
                                     SALESORDERDETAIL.TAX , 
                                      ITEM.PACKDEFINITION  AS ITEMCODE, 
                                      SALESORDERDETAIL.PACKID, 
                                     cast( (SALESORDERDETAIL.PRICE) + (((SALESORDERDETAIL.TAX / 100)* SALESORDERDETAIL.PRICE) / SALESORDERDETAIL.QUANTITY ) as numeric(19,2)) AS VALUE 
                                      FROM   SALESORDERDETAIL 
                                      INNER JOIN PACK ON SALESORDERDETAIL.PACKID = PACK.PACKID 
                                      INNER JOIN ITEM ON PACK.ITEMID = ITEM.ITEMID 
                                      WHERE  ( SALESORDERDETAIL.ORDERID = '" + TranID.ToString() + @"' ) and SALESORDERDETAIL.Quantity <> 0 ";


                    InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                    err = dtlQry.Execute();
                    err = dtlQry.FindFirst();

                    if (err != InCubeErrors.Success)
                    {
                        throw new Exception("No details found");
                    }

                    while (err == InCubeErrors.Success)
                    {
                        object SalesTransactionType = 0;
                        #region Get SalesTransaction Information
                        {
                            err = dtlQry.GetField(0, ref Quantity);
                            err = dtlQry.GetField(1, ref Price);
                            err = dtlQry.GetField(2, ref Discount);
                            err = dtlQry.GetField(3, ref Tax);
                            err = dtlQry.GetField(4, ref ItemCode);
                            err = dtlQry.GetField(5, ref PackID);
                            err = dtlQry.GetField(6, ref Value); // unit price + unit tax
                        }
                        #endregion
                        
                        decimal DiscountValue = 0;
                        decimal unitPrice = 0;
                        decimal taxvalue = 0;
                        if (Discount.ToString() != "")
                        { DiscountValue = decimal.Parse(Discount.ToString()); }
                        DiscountValue = Math.Truncate(decimal.Parse((decimal.Parse(100.0.ToString()) * DiscountValue).ToString())) / 100;

                        if (Price.ToString() != "" && decimal.Parse(Price.ToString()) != 0)
                        {
                            unitPrice = decimal.Parse(Price.ToString()) + ((decimal.Parse(Tax.ToString())/100) * decimal.Parse(Price.ToString()));
                            unitPrice = decimal.Parse(unitPrice.ToString("F2"));
                            
                        }

                        if (Tax.ToString() != "")
                        { taxvalue = decimal.Parse(Tax.ToString()); }

                        dtDate = Convert.ToDateTime(TranDate);
                        Counter++;
                        Counter2++;

                        BuildSalesOrderLine(ref SalesOrderString, Counter.ToString(), ItemCode.ToString(), Quantity.ToString(), unitPrice.ToString(), dtDate.ToString("yyyy-MM-dd"), DiscountValue.ToString());
                        //BuildStockWithdrawalLine(ref StockWithdrwalString, Counter2.ToString(), ItemCode.ToString(), Quantity.ToString(), dtDate.ToString("yyyy-MM-dd"));

                        err = dtlQry.FindNext();
                    }
                    BuildSalesOrderFooter(ref SalesOrderString);
                    // Call for items withdrawal
                    BuildStockWithdrawalFooter(ref StockWithdrwalString);

                    SalesOrderString = SalesOrderString.Replace('$', '"');
                    //StockWithdrwalString = StockWithdrwalString.Replace('$', '"');

                    string sFilePath = "";

                    string sFilePathWithdrawal = "";

                    sFilePath = FilePathSalesOrder;
                    sFilePathWithdrawal = FilePathSalesOrderWithdrwal;

                    string SalesFileName = TranID.ToString() + ".XML";
                    //string WithdrawalFileName = TranID.ToString() + "-Withdrawal.XML";

                    System.IO.File.WriteAllText(sFilePath + SalesFileName, SalesOrderString.ToString());
                    System.IO.File.WriteAllText(sFilePath + "\\Backup\\" + SalesFileName, SalesOrderString.ToString());
                    //System.IO.File.WriteAllText(sFilePathWithdrawal + "\\" + WithdrawalFileName, StockWithdrwalString.ToString());
                    //System.IO.File.WriteAllText(sFilePathWithdrawal + "\\Backup\\" + WithdrawalFileName, StockWithdrwalString.ToString());


                    QueryBuilderObject.SetField("Synchronized", "1");
                    QueryBuilderObject.UpdateQueryString("SalesOrder", " orderid = '" + TranID.ToString() + "'", db_vms);

                    QueryBuilderObject.SetField("OrderStatusid", "9");
                    QueryBuilderObject.UpdateQueryString("SalesOrder", " orderid = '" + TranID.ToString() + "'", db_vms);



                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                   WriteMessage("\r\n" + TranID.ToString() + " - FAILED!");
                }

                err = GetSalesTransactionInformation.FindNext();
            }
        }
        #endregion

        #region SendReciepts
        public override void SendReciepts()
        {
            ArrayList arUnClosedInVoices = new ArrayList();

           WriteMessage("\r\n" + "Send Payment");
            
            object field = new object();
            object DimAccount = new object();
            object CustomerPaymentID = null;
            object CustomerCode = null;
            object EmployeeCode = null;
            object PaymentDate = null;
            object AppliedAmount = null;
            object VoucherNumber = null;
            object VoucherDate = null;
            object VoucherOwner = null;
            object BankID = null;
            object BranchID = null;
            object TransactionID = null;
            object PaymentTypeID = null;
            object SalesMode = null;
            object OrgID = null;
            object WhID = null;
            object CollectionDiscount = null;
            object AppliedPaymentID = null;


            string QueryString = @"SELECT CustomerPayment.CustomerPaymentID, CustomerOutlet.CustomerCode, 
Employee.EmployeeCode, CustomerPayment.PaymentDate, CustomerPayment.AppliedAmount, CustomerPayment.VoucherNumber, 
CustomerPayment.VoucherDate, CustomerPayment.VoucherOwner, CustomerPayment.BankID, CustomerPayment.BranchID, 
CustomerPayment.TransactionID, CustomerPayment.PaymentTypeID,[Transaction].SalesMode ,[Transaction].Organizationid, Warehouse.WarehouseCode ,
ISNULL(CustomerPayment.CollectionDiscount,0) , CustomerPayment.AppliedPaymentID 
FROM CustomerOutlet
RIGHT OUTER JOIN CustomerPayment ON CustomerOutlet.OutletID = CustomerPayment.OutletID AND CustomerOutlet.CustomerID = CustomerPayment.CustomerID
INNER JOIN Employee ON CustomerPayment.EmployeeID = Employee.EmployeeID
INNER JOIN EmployeeVehicle ON CustomerPayment.EmployeeID = EmployeeVehicle.EmployeeID
INNER JOIN Warehouse ON EmployeeVehicle.VehicleID = Warehouse.WarehouseID
--INNER JOIN VehicleLoadingWh  on Warehouse.WarehouseID=VehicleLoadingWh.VehicleID
--INNER JOIN Warehouse MW ON VehicleLoadingWh.WarehouseID = MW.WarehouseID
INNER JOIN Customer ON CustomerOutlet.CustomerID = Customer.CustomerID
INNER JOIN [Transaction] ON CustomerPayment.TransactionID=[Transaction].TransactionID AND CustomerPayment.CustomerID=[Transaction].CustomerID AND CustomerPayment.OutletID=[Transaction].OutletID  
left join paymenthistory ph on ph.Customerpaymentid = CustomerPayment.CustomerPaymentID
WHERE ((CustomerPayment.Synchronized = 0) and (ph.Customerpaymentid is null) and (ph.AppliedPaymentID is null)) AND (CustomerPayment.PaymentStatusID <> 5) AND (Customer.New = 0) And 
(IsNull([Transaction].Salesmode,2) <> 1 OR [Transaction].TransactionTypeID = 3 ) And   CustomerPayment.PaymentTypeID not in (2,3, 4)
AND (CustomerPayment.PaymentDate >= '" + Filters.FromDate.Date.ToString("yyyy/MM/dd") +
   @"' AND  CustomerPayment.PaymentDate < '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"')";

            if (Filters.EmployeeID!=-1)
            {
                QueryString += " AND CustomerPayment.EmployeeID = " + Filters.EmployeeID;
            }

            InCubeQuery CustomerPaymentQuery = new InCubeQuery(db_vms, QueryString);

            CustomerPaymentQuery.Execute();
            err = CustomerPaymentQuery.FindFirst();
            int Counter = 0;
            while (err == InCubeErrors.Success)
            {
                try
                {

                    err = CustomerPaymentQuery.GetField(0, ref CustomerPaymentID);
                    err = CustomerPaymentQuery.GetField(1, ref CustomerCode);
                    err = CustomerPaymentQuery.GetField(2, ref EmployeeCode);
                    err = CustomerPaymentQuery.GetField(3, ref PaymentDate);
                    err = CustomerPaymentQuery.GetField(4, ref AppliedAmount);
                    err = CustomerPaymentQuery.GetField(5, ref VoucherNumber);
                    err = CustomerPaymentQuery.GetField(6, ref VoucherDate);
                    err = CustomerPaymentQuery.GetField(7, ref VoucherOwner);
                    err = CustomerPaymentQuery.GetField(8, ref BankID);
                    err = CustomerPaymentQuery.GetField(9, ref BranchID);
                    err = CustomerPaymentQuery.GetField(10, ref TransactionID);
                    err = CustomerPaymentQuery.GetField(11, ref PaymentTypeID);
                    err = CustomerPaymentQuery.GetField(12, ref SalesMode);
                    err = CustomerPaymentQuery.GetField(13, ref OrgID);
                    err = CustomerPaymentQuery.GetField(14, ref WhID);
                    err = CustomerPaymentQuery.GetField(15, ref CollectionDiscount);
                    err = CustomerPaymentQuery.GetField(16, ref AppliedPaymentID);
                    
                    // string CustCodeINV = "";
                    // CustCodeINV = GetCustomerTypeID(CustomerCode.ToString());
                    //MessageBox.Show(CustCodeINV);
                    //if (SalesMode.ToString().Trim() == "1")
                    //{
                    //    err = CustomerPaymentQuery.FindNext();
                    //    continue;
                    //}


                    DateTime _tranDate = DateTime.Parse(PaymentDate.ToString());

                    field = GetFieldValue("invan_To_Be_Paid", "Invoice_Num", "Order_Num = '" + TransactionID.ToString() + "'", db_ERP);
                    DimAccount = GetFieldValue("invan_Salesman", "SubAcc", "ltrim(rtrim(EmployeeCode)) = '" + EmployeeCode.ToString() + "'", db_ERP);
                    string IscalaInvoiceID = field.ToString();

                    if (IscalaInvoiceID.Trim() == string.Empty)
                    {
                        arUnClosedInVoices.Add(CustomerPaymentID.ToString());
                        err = CustomerPaymentQuery.FindNext();
                        continue;
                    }
                    Counter++;



                    StringBuilder PaymentString = new StringBuilder();
                    string CusCode = GetCustomerMasterCode(CustomerCode.ToString());

                    BuildPayments(ref PaymentString, IscalaInvoiceID, CusCode.ToString(), _tranDate.ToString("yyyy-MM-dd"), AppliedAmount.ToString(), CustomerPaymentID.ToString(), DimAccount.ToString(), "100101003");
                    PaymentString = PaymentString.Replace('$', '"');
                    string sFilePath = "";
                    sFilePath =  FilePathPayments ;
                    string FileName =TransactionID.ToString() +"-"+ AppliedPaymentID.ToString() + ".XML";
                    System.IO.File.WriteAllText(sFilePath + FileName, PaymentString.ToString());
                    System.IO.File.WriteAllText(sFilePath + "\\Backup\\" + FileName, PaymentString.ToString());

                   WriteMessage("\r\n" + CustomerPaymentID.ToString() + " - OK");

                    StringBuilder PaymentString1 = new StringBuilder();
                    if (Decimal.Parse(CollectionDiscount.ToString()) != 0)
                    {
                        BuildPayments(ref PaymentString1, IscalaInvoiceID, CusCode.ToString(), _tranDate.ToString("yyyy-MM-dd"), CollectionDiscount.ToString(), CustomerPaymentID.ToString(), DimAccount.ToString(), "400101003");
                        PaymentString1 = PaymentString1.Replace('$', '"');
                        string sFilePath1 = "";
                        sFilePath1 =  FilePathPayments;
                        string FileName1 = TransactionID.ToString() + "-" + AppliedPaymentID.ToString() + "-Discount.XML";
                        System.IO.File.WriteAllText(sFilePath1 + FileName1, PaymentString1.ToString());
                        System.IO.File.WriteAllText(sFilePath1 + "\\Backup\\" + FileName1, PaymentString1.ToString());

                       WriteMessage("\r\n" + CustomerPaymentID.ToString() + "-Discount - OK");

                    }

                    QueryBuilderObject.SetField("Synchronized", "1");
                    QueryBuilderObject.UpdateQueryString("CustomerPayment", " CustomerPaymentID = '" + CustomerPaymentID.ToString() + "'", db_vms);
                    QueryBuilderObject.SetField("Customerpaymentid", "'" + CustomerPaymentID + "'");
                    QueryBuilderObject.SetField("Customercode", "'" + CustomerCode + "'");
                    QueryBuilderObject.SetField("AppliedPaymentID", "'" + AppliedPaymentID + "'");
                    QueryBuilderObject.InsertQueryString("Paymenthistory", db_vms);
                }
                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorPay.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                   WriteMessage("\r\n" + CustomerPaymentID.ToString() + " - FAILED!");
                }

                err = CustomerPaymentQuery.FindNext();
            }
            if (arUnClosedInVoices.Count > 0)
            {
                string OpenInvoices = null;
                for (int nCounter = 0; nCounter < arUnClosedInVoices.Count; nCounter++)
                {
                    OpenInvoices += (string)arUnClosedInVoices[nCounter];
                    OpenInvoices += "\r\n";
                }
               WriteMessage("Please close the open invoices before proceeding , the current open invoices are " + OpenInvoices.ToString());
               WriteMessage("\r\n ********************************* \r\n" + "Please close the open invoices before proceeding , the current open invoices are \r\n" + OpenInvoices.ToString());

            }
        }
        #endregion

        #region SendTransfers
        public override void SendTransfers()
        {
            int Counter = 0;
            object TranID = "";
            object TranType = "";
            object TranDate = "";
            object Store = "";
            object Van = "";
            object WhID = "";
            object UOM = "";
            object ItemCode = "";
            object Quantity = "";
            object PackQty = "";
            object Operation = "";
            object OrderId = ""; 


           WriteMessage("\r\n" + "Sending Invoices");

            string QueryString = @"SELECT distinct
            WarehouseTransaction.TransactionID,
            WarehouseTransaction.TransactionDate,
            Warehouse.WarehouseCode AS ToWh,
            Warehouse_1.WarehouseCode AS FromWh,
            WarehouseTransaction.WarehouseID,
            WarehouseTransaction.TransactionOperationID , 
            WarehouseTransaction.SourceTransactionID
            FROM WarehouseTransaction
            INNER JOIN Warehouse ON WarehouseTransaction.WarehouseID = Warehouse.WarehouseID
            INNER JOIN Warehouse AS Warehouse_1 ON WarehouseTransaction.RefWarehouseID = Warehouse_1.WarehouseID
			left join SalesOrder on SalesOrder.WarehouseTransactionID = WarehouseTransaction.TransactionID
            Where 
            WarehouseTransaction.Synchronized = 0 
            AND (WarehouseTransaction.TransactionDate >= '" + Filters. FromDate.Date.ToString("yyyy/MM/dd") + @"' 
            AND  WarehouseTransaction.TransactionDate <= '" + Filters.ToDate.Date.AddDays(1).ToString("yyyy/MM/dd") + @"') 
            AND  WarehouseTransaction.TransactionOperationID in (1,2,5) AND  
			((WarehouseTransaction.WarehouseTransactionStatusID = 2 and SalesOrder.OrderID is not null) 
			or (WarehouseTransaction.WarehouseTransactionStatusID = 1 and SalesOrder.OrderID is null))";

            if (Filters.EmployeeID!=-1)
            {
                QueryString += " AND RequestedBy = " + Filters.EmployeeID;
            }

            InCubeQuery GetSalesTransactionInformation = new InCubeQuery(db_vms, QueryString);

            err = GetSalesTransactionInformation.Execute();
            err = GetSalesTransactionInformation.FindFirst();
            while (err == InCubeErrors.Success)
            {
                try
                {
                     #region Get SalesTransaction Information
                        {
                            err = GetSalesTransactionInformation.GetField(0, ref TranID);
                            err = GetSalesTransactionInformation.GetField(1, ref  TranDate);
                            err = GetSalesTransactionInformation.GetField(2, ref  Van);
                            err = GetSalesTransactionInformation.GetField(3, ref Store);
                            err = GetSalesTransactionInformation.GetField(4, ref WhID);
                            err = GetSalesTransactionInformation.GetField(5, ref Operation);
                            err = GetSalesTransactionInformation.GetField(6, ref OrderId);
                        }
                        #endregion
                        DateTime dtDate = Convert.ToDateTime(TranDate);
                        string ToWh = Van.ToString();
                        string FrmWh = Store.ToString();

                        if (Operation.Equals("5"))
                        {
                            ToWh = Store.ToString();
                            FrmWh = Van.ToString();
                        }
                    
                    StringBuilder TransferString = new StringBuilder();
                    string IscalaMaximumTransferNumber = TranID.ToString();

                    BuildTransfersHeader(ref TransferString, IscalaMaximumTransferNumber, ToWh, FrmWh, dtDate.ToString("yyyy-MM-dd"));


                    if (err == InCubeErrors.Success)
                    {
                      string dtlQryStr = @"SELECT
                      item.PACKDEFINITION ItemCode,WhTransDetail.[Quantity],PackTypeLanguage.Description UOM,pack.Quantity PackQty
                      FROM [WhTransDetail]
                      INNER JOIN pack on WhTransDetail.PackID=Pack.PackID
                      INNER JOIN Item on pack.ItemID=item.ItemID
                      Left JOIN PackTypeLanguage on pack.PackTypeID=PackTypeLanguage.PackTypeID and PackTypeLanguage.LanguageID=1
                      where WhTransDetail.WarehouseID=" + WhID + " and WhTransDetail.TransactionID='" + TranID + "'";


                        InCubeQuery dtlQry = new InCubeQuery(db_vms, dtlQryStr);
                        err = dtlQry.Execute();
                        err = dtlQry.FindFirst();

                        if (err != InCubeErrors.Success)
                        {
                            throw new Exception("No details found");
                        }

                        while (err == InCubeErrors.Success)
                        {

                            #region Get SalesTransaction Information
                            {
                                err = dtlQry.GetField(0, ref ItemCode);
                                err = dtlQry.GetField(1, ref Quantity);
                                err = dtlQry.GetField(2, ref UOM);
                                err = dtlQry.GetField(3, ref PackQty);

                            }
                            #endregion
                            Counter++;
                            UOM = GetFieldValue("invan_Unit_List", "Code", "Ltrim(Rtrim(Name)) ='" + UOM.ToString().Trim() + "'",  db_ERP);
                            BuildTransfersLine(ref TransferString,Counter.ToString(), ToWh, FrmWh, ItemCode.ToString(), UOM.ToString(), Quantity.ToString());

                            if (err != InCubeErrors.Success) break;
                            err = dtlQry.FindNext();
                        }

                        BuildTransfersFooter(ref TransferString);

                        TransferString = TransferString.Replace('$', '"');

                        string sFilePath = "";

                        sFilePath = FilePathTransfer;

                        string SalesFileName = TranID.ToString() + ".XML";

                        System.IO.File.WriteAllText(sFilePath + SalesFileName, TransferString.ToString());
                        System.IO.File.WriteAllText(sFilePath + "\\Backup\\" + SalesFileName, TransferString.ToString());

                        QueryBuilderObject.SetField("Synchronized", "1");
                        QueryBuilderObject.UpdateQueryString("WarehouseTransaction", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                        QueryBuilderObject.SetField("WarehouseTransactionStatusID", "4");
                        QueryBuilderObject.UpdateQueryString("WarehouseTransaction", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                        QueryBuilderObject.SetField("posted", "1");
                        QueryBuilderObject.UpdateQueryString("WarehouseTransaction", " TransactionID = '" + TranID.ToString() + "'", db_vms);
                        QueryBuilderObject.SetField("OrderStatusID", "3");
                        QueryBuilderObject.UpdateQueryString("SalesOrder", " WarehouseTransactionID = '" + TranID.ToString() + "'", db_vms);
                        QueryBuilderObject.SetField("WarehouseTransactionID", "Null");
                        QueryBuilderObject.UpdateQueryString("SalesOrder", " WarehouseTransactionID = '" + TranID.ToString() + "'", db_vms);


                        


                    }
                }

                catch (Exception ex)
                {
                    StreamWriter wrt = new StreamWriter("errorInv.log", true);
                    wrt.Write(ex.ToString());
                    wrt.Close();
                   WriteMessage("\r\n" + TranID.ToString() + " - FAILED!");
                }
                err = GetSalesTransactionInformation.FindNext();
            }
        }
        #endregion

        #endregion

        #region Methods

        private void DefaultOrganization()
        {
            OrganizationID = GetFieldValue("Organization", "OrganizationID", "OrganizationID = 1", db_vms);
            if (OrganizationID == string.Empty)
            {
                OrganizationID = "1";
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


        private string GetCustomerTypeID(string CustomerCode)
        {
            object field = new object();

            field = GetFieldValue("CustomerOutlet", "CustomerTypeID", "CustomerCode = '" + CustomerCode + "'", db_vms);

            return field.ToString();
        }

        private string GetCustomerMasterCode(string CustomerCode)
        {
            object field = new object();
            object field2 = new object();

            field2 = GetFieldValue("CustomerOutlet", "CustomerID", "CustomerCode = '" + CustomerCode + "'", db_vms);

            field = GetFieldValue("Customer", "CustomerCode", "CustomerID = " + Convert.ToInt32(field2) + "", db_vms);

            return field.ToString();
        }

        private string VerifyInvoiceExistance(string InVanInvoiceID)
        {
            object field = new object();

            field = GetFieldValue("VScalaInvoiceValidation", "[InCube Reference]", "[InCube Reference] = '" + InVanInvoiceID + "'", db_ERP);

            return field.ToString();
        }
        
        #endregion

    }

}




