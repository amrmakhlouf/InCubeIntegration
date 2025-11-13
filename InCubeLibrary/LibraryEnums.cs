
namespace InCubeLibrary
{
    public enum Menus
    {
        Manual_Integration = 1,
        Schedules_Management = 2,
        Users_Access = 3,
        Operations_Monitoring = 4,
        Employees_Importing = 5,
        Targets_Importing = 6,
        Integration_Update = 7,
        Integration_Send = 8,
        Integration_Configurations = 9,
        Load_Request = 10,
        Excel_Import = 11,
        Transactions_Management = 12,
        Mail_Configuration = 13,
        Integration_SpecialActions = 14,
        Process_Returns = 15,
        RoadNet_Integration = 16,
        RoadNet_Export = 17,
        RoadNet_Import = 18,
        Route_Region = 19,
        StandardInstructions = 20,
        PRN_Config = 21,
        Locations_Export = 22,
        JorneyPlan_Import = 23
    }

    public enum MenuActions
    {
        RoadNet_Export_HomeAndOffice = 1,
        RoadNet_Export_Delivery = 2
    }

    public enum ActionType
    {
        Update = 1,
        Send = 2,
        SpecialFunctions = 3
    }

    public enum IntegrationField
    {
        Item_U = 1,
        Customer_U = 2,
        Price_U = 3,
        Discount_U = 4,
        Route_U = 5,
        Invoice_U = 6,
        KPI_U = 7,
        STA_U = 8,
        EDI_U = 9,
        Stock_U = 10,
        Orders_S = 11,
        Reciept_S = 12,
        ATM_S = 13,
        STP_U = 14,
        PackGroup_U = 15,
        CNT_U = 16,
        Outstanding_U = 17,
        MainWarehouseStock_U = 18,
        Vehicles_U = 19,
        Warehouse_U = 20,
        Salesperson_U = 21,
        GeoLocation_U = 22,
        Sales_S = 23,
        Transfers_S = 24,
        Returns_S = 25,
        OrderInvoice_S = 26,
        NewCustomer_S = 27,
        DownPayment_S = 28,
        Promotion_U = 29,
        StockInterface_S = 30,
        InvoiceInterface_S = 31,
        Orders_U = 32,
        FilesJobs_SP = 33,
        NewCustomer_U = 34,
        Target_U = 35,
        POSM_U = 36,
        ContractedFOC_U = 37,
        Price_S = 38,
        Promotion_S = 39,
        Bank_U = 40,
        DataBaseActions_SP = 41,
        CreditNoteRequest_S = 42,
        DataTransfer_SP = 43,
        DatabaseBackup_SP = 44,
        ExportImages_SP = 45,
        SerialStock_U = 46,
        Areas_U = 47,
        RoadNetImport_U = 48,
        ExtractTransactionsMapImages_SP = 49,
        DataWarehouse_SP = 50
    }
    public enum BuiltInFilters
    {
        Organization = 1,
        Employee = 2,
        CustomerCode = 3,
        Warehouse = 4,
        StockDate = 5,
        FromDate = 6,
        ToDate = 7,
        //TriggerID = 8,
        //UserID = 9,
        DataTransferCheckList = 10,
        FilesManagementJobs = 11,
        DatabaseBackupJob = 12,
        ExtraSendFilter = 13,
        OpenInvoicesOnly = 14,
        TextSearch = 15,
        DataWarehouseCheckList = 16
    }
    public enum Result
    {
        UnKnown = 0,
        Success = 1,
        Failure = 2,
        NoRowsFound = 3,
        Invalid = 4,
        InActive = 5,
        LoggedIn = 6,
        WebServiceConnectionError = 7,
        NoFileRetreived = 8,
        Duplicate = 9,
        ErrorExecutingQuery = 10,
        Started = 11,
        NotInitialized = 12,
        Blocked = 13
    }

    public enum ConfigurationType
    {
        String = 1,
        Boolean = 2,
        Long = 3,
        Color = 4
    }

    public enum LoginType
    {
        WindowsService,
        User,
        NoLoginForm
    }

    public enum ParamType
    {
        Integer = 1,
        Nvarchar = 2,
        DateTime = 3,
        BIT = 4,
        Decimal = 5
    }

    public enum ParamDirection
    {
        Input = 1,
        Output = 2
    }

    public enum ProcType
    {
        SQLProcedure = 1,
        SMS = 2,
        Mail = 3,
        ExcelExport = 4,
        DataTransfer = 5,
        OracleProcedure = 6
    }

    public enum ColumnType
    {
        Int = 0,
        String = 1,
        Decimal = 2,
        Datetime = 3,
        Bool = 4,
        Image = 5
    }

    public enum BuildMode
    {
        Live = 1,
        Test = 0,
        Live2 = 2
    }

    public enum DataBaseType
    {
        SQLServer = 1,
        Oracle = 2
    }
    public enum TransferMethod
    {
        DeleteAndInsert = 1,
        InsertAndUpdate = 2,
        InsertOnly = 3,
        UpdateOnly = 4
    }

    public enum TaskStatus
    {
        Active = 1,
        Stopped = 2,
        Deleted = 3,
        Changed = 4
    }

    public enum ScheduleType
    {
        DailyEvery = 1,
        DailyAt = 2,
        Weekly = 3,
        Monthly = 4
    }

    public enum FilterType
    {
        Date = 1,
        DateTime = 2,
        Time = 3,
        Text = 4,
        ComboBox = 5,
        CheckBox = 6
    }

    public enum Priority
    {
        High = 1,
        Medium,
        Low
    }
    public enum PrivilegeType
    {
        MenuAccess = 1,
        FieldAccess = 2,
        ExcelImport = 3,
        MenuAction = 4
    }
    public enum FormMode
    {
        View,
        Add,
        Edit,
        Import,
        Export
    }
    public enum MailRecipientType
    {
        To = 1,
        CC = 2,
        BCC = 3
    }
    public enum FileJobType
    {
        Delete = 1,
        Move = 2,
        Copy = 3
    }
    public enum AgeTimeUnit
    {
        Second = 1,
        Minute = 2,
        Hour = 3,
        Day = 4,
        Month = 5,
        Year = 6
    }

    public enum Queues_Mode
    {
        Org_Action = 1,
        TaskID = 2
    }

    public enum SaveMode
    {
        ToDatabase,
        ToScript
    }

    public enum ComparisonOperator
    {
        EqualTo = 1,
        GreaterThan = 2,
        GreaterThanOrEqualTo = 3,
        LessThan = 4,
        LessThanOrEqualTo = 5
    }
    public enum ServiceStatus
    {
        UnKnown = 0,
        NotInstalled = 1,
        Running = 2,
        Stopped = 3,
        Disabled = 4
    }
    public enum TransferTypes
    {
        DataTransfer = 1,
        DataWarehouse = 2
    }

    public enum AttachmentType
    {
        ExcelFromQuery = 1
    }
}
