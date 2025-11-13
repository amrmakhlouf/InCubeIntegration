using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace InCubeIntegration_BL
{
    public class ExecutionManager : System.IDisposable
    {
        InCubeQuery incubeQuery;
        public InCubeDatabase db_exec;
        public ActionType Action_Type;
        public ExecutionManager()
        {
            db_exec = new InCubeDatabase();
            db_exec.Open("InCube", "ExecutionManager");
        }
        public IntegrationBase InitializeIntegrationObject()
        {
            IntegrationBase IntegrationObj = null;
            long CurrentUserID = CoreGeneral.Common.CurrentSession.EmployeeID;
            try
            {
                switch (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower())
                {
                    case "brf":
                        IntegrationObj = new IntegrationBRF(CurrentUserID, this);
                        break;
                    case "areen":
                        IntegrationObj = new IntegrationAreen(CurrentUserID, this);
                        break;
                    case "ain":
                        IntegrationObj = new IntegrationAIN(CurrentUserID, this);
                        break;
                    case "ainfoodtruck":
                        IntegrationObj = new IntegrationAINFoodTruck(CurrentUserID, this);
                        break;
                    case "esf":
                        IntegrationObj = new IntegrationESF_Old(CurrentUserID, this);
                        break;
                    case "esfnew":
                        IntegrationObj = new IntegrationESF(CurrentUserID, this);
                        break;
                    case "abuissa":
                        IntegrationObj = new IntegrationAbuIssa(CurrentUserID, this);
                        break;
                    case "hassani":
                        IntegrationObj = new IntegrationHassani(CurrentUserID, this);
                        break;
                    case "awal":
                    case "awalkwt":
                    case "awalksa":
                        IntegrationObj = new IntegrationAwal(CurrentUserID, this);
                        break;
                    case "vodafone":
                        IntegrationObj = new IntegrationVodafone(CurrentUserID, this);
                        break;
                    case "qnie_presales":
                        IntegrationObj = new IntegrationQNIEPresales(CurrentUserID, this);
                        break;
                    case "qnie":
                        IntegrationObj = new IntegrationQNIE(CurrentUserID, this);
                        break;
                    case "masafi":
                        IntegrationObj = new IntegrationMasafi(CurrentUserID, this);
                        break;
                    case "delmonte":
                        IntegrationObj = new IntegrationDelMonte(CurrentUserID, this);
                        break;
                    case "delmontejo":
                        IntegrationObj = new IntegrationDelMonteJordan(CurrentUserID, this);
                        break;
                    case "ufc":
                        IntegrationObj = new IntegrationUnitedFoods(CurrentUserID, this);
                        break;
                    case "unitra":
                        IntegrationObj = new IntegrationUnitra(CurrentUserID, this);
                        break;
                    case "jeema":
                        IntegrationObj = new IntegrationJeema(CurrentUserID, this);
                        break;
                    case "anabtawi":
                        IntegrationObj = new IntegrationAnabtawi(CurrentUserID, this);
                        break;
                    case "pepsipal":
                        IntegrationObj = new IntegrationPepsiPal(CurrentUserID, this);
                        break;
                    case "shawar":
                        IntegrationObj = new IntegrationShawar(CurrentUserID, this);
                        break;
                    case "nedm":
                        IntegrationObj = new IntegrationNEDM(CurrentUserID, this);
                        break;
                    case "cezar":
                        IntegrationObj = new IntegrationCezar(CurrentUserID, this);
                        break;
                    case "attar":
                        IntegrationObj = new IntegrationAttar(CurrentUserID, this);
                        break;
                    case "telelink":
                        IntegrationObj = new IntegrationTeleLink(CurrentUserID, this);
                        break;
                    case "khraim":
                        IntegrationObj = new IntegrationKhraim(CurrentUserID, this);
                        break;
                    case "hammodeh":
                        IntegrationObj = new IntegrationHammodeh(CurrentUserID, this);
                        break;
                    case "seniora":
                        IntegrationObj = new IntegrationSeniora(CurrentUserID, this);
                        break;
                    default:
                        IntegrationObj = new IntegrationBase(this);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return IntegrationObj;
        }
        public Result TriggerAction(ActionType _actionType, int FieldID, IntegrationFilters Filters, int TaskID, int ActionID, int TriggerID, IntegrationBase IntegrationObj)
        {
            Result res = Result.UnKnown;
            try
            {
                if (!IntegrationObj.Initialized)
                    return Result.NotInitialized;

                IntegrationObj.TaskID = TaskID;
                IntegrationObj.ActionID = ActionID;
                IntegrationObj.TriggerID = TriggerID;
                IntegrationObj.OrgTriggerID = TriggerID;
                IntegrationObj.Filters = Filters;
                IntegrationObj.FieldID = FieldID;

                if (_actionType == ActionType.Update && IntegrationObj.CommonMastersUpdate)
                {
                    IntegrationObj.GetMasterData();
                }
                else
                {
                    switch ((IntegrationField)FieldID)
                    {
                        case IntegrationField.Item_U:
                            IntegrationObj.UpdateItem();
                            break;
                        case IntegrationField.Customer_U:
                            IntegrationObj.UpdateCustomer();
                            break;
                        case IntegrationField.Price_U:
                            IntegrationObj.UpdatePrice();
                            break;
                        case IntegrationField.Discount_U:
                            IntegrationObj.UpdateDiscount();
                            break;
                        case IntegrationField.Route_U:
                            IntegrationObj.UpdateRoutes();
                            break;
                        case IntegrationField.Invoice_U:
                            IntegrationObj.UpdateInvoice();
                            break;
                        case IntegrationField.CNT_U:
                            IntegrationObj.StoreInvoices();
                            break;
                        case IntegrationField.KPI_U:
                            IntegrationObj.UpdateKPI();
                            break;
                        case IntegrationField.STA_U:
                            IntegrationObj.UpdateSTA();
                            break;
                        case IntegrationField.EDI_U:
                            IntegrationObj.UpdateEDI();
                            break;
                        case IntegrationField.Outstanding_U:
                            IntegrationObj.OutStanding();
                            break;
                        case IntegrationField.MainWarehouseStock_U:
                            IntegrationObj.UpdateMainWHStock();
                            break;
                        case IntegrationField.Vehicles_U:
                            IntegrationObj.UpdateWarehouse();
                            break;
                        case IntegrationField.Warehouse_U:
                            IntegrationObj.UpdateMainWarehouse();
                            break;
                        case IntegrationField.Salesperson_U:
                            IntegrationObj.UpdateSalesPerson();
                            break;
                        case IntegrationField.GeoLocation_U:
                            IntegrationObj.UpdateGeographicalLocation();
                            break;
                        case IntegrationField.Stock_U:
                            IntegrationObj.UpdateStock();
                            break;
                        case IntegrationField.STP_U:
                            IntegrationObj.UpdateSTP();
                            break;
                        case IntegrationField.Promotion_U:
                            IntegrationObj.UpdatePromotion();
                            break;
                        case IntegrationField.Orders_U:
                            IntegrationObj.UpdateOrders();
                            break;
                        case IntegrationField.Sales_S:
                            IntegrationObj.SendInvoices();
                            break;
                        case IntegrationField.Reciept_S:
                            IntegrationObj.SendReciepts();
                            break;
                        case IntegrationField.Transfers_S:
                            IntegrationObj.SendTransfers();
                            break;
                        case IntegrationField.Orders_S:
                            IntegrationObj.SendOrders();
                            break;
                        case IntegrationField.CreditNoteRequest_S:
                            IntegrationObj.SendCreditNoteRequest();
                            break;
                        case IntegrationField.ATM_S:
                            IntegrationObj.SendATMCollections();
                            break;
                        case IntegrationField.Returns_S:
                            IntegrationObj.SendReturn();
                            break;
                        case IntegrationField.OrderInvoice_S:
                            IntegrationObj.SendOrderInvoices();
                            break;
                        case IntegrationField.NewCustomer_S:
                            IntegrationObj.SendNewCustomers();
                            break;
                        case IntegrationField.DownPayment_S:
                            IntegrationObj.SendDownPayments();
                            break;
                        case IntegrationField.StockInterface_S:
                            IntegrationObj.StockInterface();
                            break;
                        case IntegrationField.InvoiceInterface_S:
                            IntegrationObj.InvoiceInterface();
                            break;
                        case IntegrationField.NewCustomer_U:
                            IntegrationObj.UpdateNewCustomer();
                            break;
                        case IntegrationField.Target_U:
                            IntegrationObj.UpdateTarget();
                            break;
                        case IntegrationField.POSM_U:
                            IntegrationObj.UpdatePOSM();
                            break;
                        case IntegrationField.ContractedFOC_U:
                            IntegrationObj.UpdateContractedFOC();
                            break;
                        case IntegrationField.Price_S:
                            IntegrationObj.SendPrice();
                            break;
                        case IntegrationField.Promotion_S:
                            IntegrationObj.SendPromotion();
                            break;
                        case IntegrationField.FilesJobs_SP:
                            res = IntegrationObj.RunFilesManagementJobs();
                            break;
                        case IntegrationField.Bank_U:
                            IntegrationObj.UpdateBank();
                            break;
                        case IntegrationField.PackGroup_U:
                            IntegrationObj.UpdatePackGroup();
                            break;
                        case IntegrationField.DataTransfer_SP:
                            IntegrationObj.RunDataTransfer();
                            break;
                        case IntegrationField.DatabaseBackup_SP:
                            res = IntegrationObj.RunDatabaseBackup();
                            break;
                        case IntegrationField.ExportImages_SP:
                            res = IntegrationObj.ExportImages();
                            break;
                        case IntegrationField.Areas_U:
                            IntegrationObj.UpdateAreas();
                            break;
                        case IntegrationField.ExtractTransactionsMapImages_SP:
                            IntegrationObj.RunExtractTransactionsMapImages();
                            break;
                    }
                }
                if (!IntegrationObj.Initialized)
                    return Result.NotInitialized;

                List<Procedure> Procs = new List<Procedure>();
                if (GetFieldProcedureDetails(FieldID, ref Procs) == Result.Success)
                    IntegrationObj.ExecuteFunction(_actionType, (IntegrationField)FieldID, Procs);

                IntegrationObj.RunPostActionFunction();

                if (res == Result.UnKnown)
                    res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetAllFieldsFilters(ref DataTable dtFieldsFilters)
        {
            Result res = Result.Failure;
            try
            {
                incubeQuery = new InCubeQuery(db_exec, "SELECT FieldID,FilterID FROM Int_FieldFilters");
                dtFieldsFilters = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtFieldsFilters = incubeQuery.GetDataTable();
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetAllFields(ref DataTable dtFields)
        {
            Result res = Result.Failure;
            try
            {
                incubeQuery = new InCubeQuery(db_exec, @"SELECT F.FieldID,AT.Value [Type],F.FieldName
,CASE (SELECT COUNT(*) FROM Int_FieldProcedure WHERE FieldID = F.FieldID) WHEN 0 THEN 0 ELSE 1 END ProcDefined
FROM Int_Field F
INNER JOIN Int_Lookups AT ON AT.ID = F.ActionType AND LookupName = 'ActionType'");
                dtFields = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtFields = incubeQuery.GetDataTable();
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetFieldFilters(IntegrationField field, ref List<BuiltInFilters> filters)
        {
            Result res = Result.Failure;
            try
            {
                incubeQuery = new InCubeQuery(db_exec, "SELECT FilterID FROM Int_FieldFilters WHERE FieldID = " + field.GetHashCode());
                DataTable dtFilters = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtFilters = incubeQuery.GetDataTable();
                    filters = new List<BuiltInFilters>();
                    foreach (DataRow dr in dtFilters.Rows)
                        filters.Add((BuiltInFilters)int.Parse(dr["FilterID"].ToString()));
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetFieldProcedureDetails(int FieldID, ref List<Procedure> Procs)
        {
            Result res = Result.Failure;
            try
            {
                DataTable dtProcs = new DataTable();
                string qry = string.Format(@"SELECT P.ProcedureName,ISNULL(F.ParameterName,'') ParameterName,ISNULL(F.ParameterValue,'') ParameterValue
,ISNULL(F.ParameterType,0) ParameterType, P.ProcedureType, P.ExecutionTableName, ISNULL(P.ReadExecutionDetails,0) ReadExecutionDetails, LTRIM(RTRIM(ISNULL(P.ExecDetailsReadQry,''))) ExecDetailsReadQry, ISNULL(P.MailTemplateID,0) MailTemplateID, ISNULL(DataTransferGroupID,0) DataTransferGroupID, ISNULL(ConnectionID,0) ConnectionID
FROM Int_FieldProcedure P
LEFT JOIN Int_FieldProcParams F ON F.FieldID = P.FieldID AND F.Sequence = P.Sequence
WHERE P.FieldID = {0}
ORDER BY P.Sequence,P.ProcedureType,P.ProcedureName", FieldID);

                incubeQuery = new InCubeQuery(db_exec, qry);

                dtProcs = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtProcs = incubeQuery.GetDataTable();
                    if (dtProcs.Rows.Count > 0)
                    {
                        Dictionary<string, Procedure> AddedProcs = new Dictionary<string, Procedure>();
                        Procedure proc = new Procedure();
                        Parameter param = new Parameter();
                        string ProcName = "";
                        for (int i = 0; i < dtProcs.Rows.Count; i++)
                        {
                            ProcName = dtProcs.Rows[i]["ProcedureName"].ToString();
                            ProcType type = (ProcType)Convert.ToInt16(dtProcs.Rows[i]["ProcedureType"]);
                            if (!AddedProcs.ContainsKey(ProcName))
                            {
                                proc = new Procedure();
                                proc.ProcedureName = ProcName;
                                proc.ProcedureType = type;
                                proc.ReadExecutionDetails = bool.Parse(dtProcs.Rows[i]["ReadExecutionDetails"].ToString());
                                if (dtProcs.Rows[i]["ExecDetailsReadQry"].ToString() != string.Empty)
                                    proc.ExecDetailsReadQry = dtProcs.Rows[i]["ExecDetailsReadQry"].ToString();
                                if (dtProcs.Rows[i]["ExecutionTableName"] != DBNull.Value && !string.IsNullOrEmpty(dtProcs.Rows[i]["ExecutionTableName"].ToString()))
                                {
                                    proc.ExecutionTableName = dtProcs.Rows[i]["ExecutionTableName"].ToString();
                                }
                                proc.MailTemplateID = int.Parse(dtProcs.Rows[i]["MailTemplateID"].ToString());
                                proc.DataTransferGroupID = int.Parse(dtProcs.Rows[i]["DataTransferGroupID"].ToString());
                                proc.ConnectionID = int.Parse(dtProcs.Rows[i]["ConnectionID"].ToString());
                                AddedProcs.Add(ProcName, proc);
                            }
                            if (dtProcs.Rows[i]["ParameterName"].ToString() != string.Empty)
                            {
                                param = new Parameter();
                                param.ParameterName = dtProcs.Rows[i]["ParameterName"].ToString();
                                param.ParameterValue = dtProcs.Rows[i]["ParameterValue"].ToString();
                                param.ParameterType = (ParamType)(int.Parse(dtProcs.Rows[i]["ParameterType"].ToString()));
                                AddedProcs[ProcName].Parameters.Add(param.ParameterName, param);
                            }
                        }
                        Procs = new List<Procedure>(AddedProcs.Values);
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }

        //User Access
        public Result UpdateSequence(int PrivilegeID, int UserID, int ParentID, int PrivilegeType, int Sequence, int Change)
        {
            Result res = Result.Failure;
            try
            {
                string query = string.Format(@"
UPDATE UP SET Sequence = {5}
FROM Int_UserPrivileges UP
INNER JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType
WHERE UP.UserID = {1} AND P.ParentID = {2} AND P.PrivilegeType = {3} AND UP.Sequence = {4};
UPDATE Int_UserPrivileges SET Sequence = {4}
WHERE UserID = {1} AND PrivilegeID = {0} AND PrivilegeType = {3};"

, PrivilegeID, UserID, ParentID, PrivilegeType, Sequence, Sequence - Change);
                incubeQuery = new InCubeQuery(db_exec, query);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result UpdateDefaultCheck(int PrivilegeID, int UserID, int PrivilegeType, bool DefaultCheck)
        {
            Result res = Result.Failure;
            try
            {
                string query = string.Format(@"
UPDATE Int_UserPrivileges SET DefaultCheck = {3} WHERE UserID = {1} AND PrivilegeID = {0} AND PrivilegeType = {2};"
, PrivilegeID, UserID, PrivilegeType, DefaultCheck ? 1 : 0);
                incubeQuery = new InCubeQuery(db_exec, query);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result AddRemoveUserPrivilges(int UserID, int PrivilegeID, int PrivilegeType, bool Add, int ParentID, int Sequence, bool DefaultCheck)
        {
            Result res = Result.Failure;
            try
            {
                string query = "";
                if (Add)
                {
//                    incubeQuery = new InCubeQuery(db_exec, string.Format(@"SELECT ISNULL(MAX(Sequence),0) + 1 
//FROM Int_UserPrivileges UP
//INNER JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType
//WHERE UP.PrivilegeType = {0} AND P.ParentID = {1} AND UP.UserID = {2}", PrivilegeType, ParentID, UserID));
//                    object field = null;
//                    if (incubeQuery.ExecuteScalar(ref field) == InCubeErrors.Success)
//                        Sequence = int.Parse(field.ToString());
                    query = string.Format(@"IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = {0} AND PrivilegeID = {1} AND PrivilegeType = {2})
BEGIN
    UPDATE UP SET Sequence = Sequence + 1
    FROM Int_UserPrivileges UP
    INNER JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType
    WHERE UP.UserID = {0} AND P.ParentID = {4} AND P.PrivilegeType = {2} AND UP.Sequence >= {3}	
    INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence,DefaultCheck) VALUES ({0},{1},{2},{3},{5});
    
END", UserID, PrivilegeID, PrivilegeType, Sequence, ParentID, DefaultCheck ? 1 : 0);
                }
                else
                {
                    query = string.Format(@"DELETE FROM Int_UserPrivileges WHERE UserID = {0} AND PrivilegeID = {1} AND PrivilegeType = {2}
UPDATE UP SET Sequence = Sequence - 1
FROM Int_UserPrivileges UP
INNER JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType
WHERE UP.UserID = {0} AND P.ParentID = {4} AND P.PrivilegeType = {2} AND UP.Sequence > {3}	", UserID, PrivilegeID, PrivilegeType, Sequence, ParentID);
                }
                incubeQuery = new InCubeQuery(db_exec, query);
                res = incubeQuery.ExecuteNonQuery() == InCubeErrors.Success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public Result GetUserPrivileges(int UserID, bool IsAdminView, ref DataTable dtPrivileges)
        {
            Result res = Result.Failure;
            try
            {
                string qry = "";
                if (IsAdminView)
                {
                    qry = @"SELECT T.*,ISNULL(UP.Sequence,0) Sequence
,CASE WHEN UP.PrivilegeID IS NULL THEN 0 ELSE 1 END HasAccess
, ISNULL(UP.DefaultCheck,0) DefaultCheck
FROM (
SELECT P.PrivilegeID,P.PrivilegeType
,CASE P.PrivilegeType WHEN 1 THEN P.Description WHEN 2 THEN F.FieldName WHEN 3 THEN I.Name END Name
,CASE P.PrivilegeType WHEN 3 THEN 11 ELSE P.ParentID END ParentID
FROM Int_Privileges P
LEFT JOIN Int_Field F ON F.FieldID = P.PrivilegeID AND P.PrivilegeType = 2
LEFT JOIN Int_ExcelImportTypes I ON I.ImportTypeID = P.PrivilegeID AND P.PrivilegeType = 3) T
LEFT JOIN Int_UserPrivileges UP ON UP.PrivilegeID = T.PrivilegeID 
AND UP.PrivilegeType = T.PrivilegeType AND UP.UserID = 0
ORDER BY T.ParentID, ISNULL(UP.Sequence,999), T.PrivilegeID";
                }
                else
                {
                    qry = string.Format(@"SELECT T.*,ISNULL(UP.Sequence,0) Sequence
,CASE WHEN UP.PrivilegeID IS NULL THEN 0 ELSE 1 END HasAccess
, ISNULL(UP.DefaultCheck,0) DefaultCheck
FROM (
SELECT P.PrivilegeID,P.PrivilegeType
,CASE P.PrivilegeType WHEN 1 THEN P.Description WHEN 2 THEN F.FieldName WHEN 3 THEN I.Name END Name
,CASE P.PrivilegeType WHEN 3 THEN 11 ELSE P.ParentID END ParentID, AP.Sequence AdminSequence
FROM Int_Privileges P
INNER JOIN Int_UserPrivileges AP ON AP.PrivilegeID = P.PrivilegeID 
AND AP.PrivilegeType = P.PrivilegeType AND AP.UserID = {0}
LEFT JOIN Int_Field F ON F.FieldID = P.PrivilegeID AND P.PrivilegeType = 2
LEFT JOIN Int_ExcelImportTypes I ON I.ImportTypeID = P.PrivilegeID AND P.PrivilegeType = 3) T
LEFT JOIN Int_UserPrivileges UP ON UP.PrivilegeID = T.PrivilegeID 
AND UP.PrivilegeType = T.PrivilegeType AND UP.UserID = {1}
ORDER BY T.ParentID, ISNULL(UP.Sequence,999), AdminSequence, T.PrivilegeID", CoreGeneral.Common.CurrentSession.UserID, UserID);
                }
                incubeQuery = new InCubeQuery(db_exec, qry);
                dtPrivileges = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtPrivileges = incubeQuery.GetDataTable();
                    if (dtPrivileges.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetIntegrationUsersList(ref DataTable dtIntegrationEmployees)
        {
            Result res = Result.Failure;
            try
            {
                string qry = string.Format(@"SELECT EO.OperatorID UserID, E.EmployeeCode UserCode, El.Description UserName 
FROM Employee E 
INNER JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID AND EL.LanguageID = 1
INNER JOIN EmployeeOperator EO ON EO.EmployeeID = E.EmployeeID
WHERE E.EmployeeTypeID <> 2 AND E.Inactive = 0 AND E.OrganizationID IN ({0}) AND E.EmployeeID <> 0", CoreGeneral.Common.userPrivileges.Organizations);

                incubeQuery = new InCubeQuery(db_exec, qry);

                dtIntegrationEmployees = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtIntegrationEmployees = incubeQuery.GetDataTable();
                    if (dtIntegrationEmployees.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        
        //Logging
        public int LogIntegrationBegining(int TriggerID, int OrganizationID, Dictionary<int, string> filters)
        {
            int id = 0;
            object ID = null;
            try
            {
                string insertQuery = "INSERT INTO Int_ExecutionDetails (TriggerID,OrganizationID,";
                string valuesQuery = string.Format(" VALUES ({0},{1},", TriggerID, OrganizationID);
                int No = 1;
                if (filters != null)
                {
                    foreach (KeyValuePair<int, string> filter in filters)
                    {
                        insertQuery += string.Format("Filter{0}ID,Filter{0}Value,", No++);
                        valuesQuery += string.Format("{0},'{1}',", filter.Key, filter.Value);
                        if (No == 4)
                            break;
                    }
                }
                insertQuery += "RunTimeStart)";
                valuesQuery += "GETDATE()); SELECT SCOPE_IDENTITY();";

                incubeQuery = new InCubeQuery(db_exec, insertQuery + valuesQuery);
                incubeQuery.ExecuteScalar(ref ID);
                id = int.Parse(ID.ToString());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return id;
        }
        public void LogFilePath(int ID, string FilePath)
        {
            try
            {
                string updateStm = string.Format("UPDATE Int_ExecutionDetails SET FilePath = '{0}' WHERE ID = {1}", FilePath, ID);
                incubeQuery = new InCubeQuery(db_exec, updateStm);
                InCubeErrors err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void LogIntegrationEnding(int ID, DateTime EndTime, Result result, int inserted, int updated, int skipped, string FilePath, string message)
        {
            try
            {
                string updateStm = "UPDATE Int_ExecutionDetails SET RunTimeEnd = ";
                if (EndTime == DateTime.MinValue)
                    updateStm += "GETDATE(), ";
                else
                    updateStm += string.Format("CONVERT(datetime,'{0}' ,121), ", EndTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                updateStm += string.Format("ResultID = {0}, ", result.GetHashCode());

                if (inserted != -1)
                    updateStm += string.Format("Inserted = {0}, ", inserted);
                if (updated != -1)
                    updateStm += string.Format("Updated = {0}, ", updated);
                if (skipped != -1)
                    updateStm += string.Format("Skipped = {0}, ", skipped);

                updateStm += string.Format("FilePath = '{0}', [Message] = '{1}' WHERE ID = {2}", FilePath, message.Replace("'", "''"), ID);

                incubeQuery = new InCubeQuery(db_exec, updateStm);
                InCubeErrors err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void LogIntegrationEnding(int ID)
        {
            LogIntegrationEnding(ID, DateTime.MinValue, Result.UnKnown, -1, -1, -1, "", "");
        }
        public void LogIntegrationEnding(int ID, Result result, string FilePath, string message)
        {
            LogIntegrationEnding(ID, DateTime.MinValue, result, -1, -1, -1, FilePath, message);
        }
        public void LogIntegrationEnding(int ID, Result result, long inserted, string FilePath, string message)
        {
            LogIntegrationEnding(ID, DateTime.MinValue, result, (int)inserted, -1, -1, FilePath, message);
        }
        public void LogIntegrationEnding(int ID, Result result, int inserted, int updated, string FilePath)
        {
            LogIntegrationEnding(ID, DateTime.MinValue, result, inserted, updated, -1, FilePath, "");
        }
        public void LogIntegrationEnding(int ID, Result result, int inserted, int updated, int skipped, string message)
        {
            LogIntegrationEnding(ID, DateTime.MinValue, result, inserted, updated, skipped, "", message);
        }
        public int LogSMSRequestStart(int TriggerID, string ProcedureName, string URL, string UserName, string Password, string Sender, string Mobile, string Contents, string Request)
        {
            int id = 0;
            object ID = null;
            try
            {
                string insertQuery = string.Format(@"INSERT INTO SMSSendingLog (TriggerID,ProcedureName,URL,UserName,Password,Sender,Mobile,Contents,Request,SendingTime)
                    VALUES ({0},'{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}',GETDATE());
                    SELECT SCOPE_IDENTITY()", TriggerID, ProcedureName, URL, UserName, Password, Sender, Mobile, Contents.Replace("'", "''"), Request.Replace("'", "''"));
                incubeQuery = new InCubeQuery(db_exec, insertQuery);
                incubeQuery.ExecuteScalar(ref ID);
                id = int.Parse(ID.ToString());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return id;
        }
        public void LogSMSRequestEnd(int LogID, string response)
        {
            try
            {
                string updateStm = "UPDATE SMSSendingLog SET Response = '" + response.Replace("'", "''") + "' WHERE ID = " + LogID;
                incubeQuery = new InCubeQuery(db_exec, updateStm);
                InCubeErrors err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void UpdateActionTotalRows(int TriggerID, int TotalRows)
        {
            try
            {
                string updateQuery = string.Format(@"UPDATE Int_ActionTrigger SET TotalRows = ISNULL(TotalRows,0) + {0} WHERE ID = {1}", TotalRows, TriggerID);
                incubeQuery = new InCubeQuery(db_exec, updateQuery);
                incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void UpdateActionInputOutput(int TriggerID, string MainLoopQuery, int TotalRows)
        {
            try
            {
                string updateQuery = string.Format(@"UPDATE Int_ActionTrigger SET TotalRows = ISNULL(TotalRows,0) + {0}, MainLoopQuery = '{1}' WHERE ID = {2}", TotalRows, MainLoopQuery.Replace("'", "''"), TriggerID);
                incubeQuery = new InCubeQuery(db_exec, updateQuery);
                incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public void UpdateActionMainQuery(int TriggerID, string MainLoopQuery)
        {
            try
            {
                string updateQuery = string.Format(@"UPDATE Int_ActionTrigger SET MainLoopQuery = '{0}' WHERE ID = {1}", MainLoopQuery, TriggerID);
                incubeQuery = new InCubeQuery(db_exec, updateQuery);
                incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        
        public int LogActionTriggerBegining(int TaskID, int ActionID, int FieldID)
        {
            int id = -1;
            object ID = null;
            try
            {
                string insertQuery = string.Format(@"INSERT INTO Int_ActionTrigger (SessionID,TaskID,ActionID,FieldID,RunTimeStart) VALUES ({0},{1},{2},{3},GETDATE());
                    SELECT SCOPE_IDENTITY();", CoreGeneral.Common.CurrentSession.SessionID, TaskID, ActionID, FieldID);
                incubeQuery = new InCubeQuery(db_exec, insertQuery);
                incubeQuery.ExecuteScalar(ref ID);
                id = int.Parse(ID.ToString());
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return id;
        }
        public void LogActionTriggerEnding(int ID)
        {
            try
            {
                string updateQuery = string.Format("UPDATE Int_ActionTrigger SET RunTimeEnd = GETDATE() WHERE ID = {0}", ID);
                incubeQuery = new InCubeQuery(db_exec, updateQuery);
                InCubeErrors err = incubeQuery.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        //Files Management
        public Result Compress(string SourceFolder, string ZipFilePath, int level, bool CopyFolder, bool DeleteSource)
        {
            Result res = Result.UnKnown;
            try
            {
                int TrimLength = 0;
                if (CopyFolder)
                    TrimLength = Directory.GetParent(SourceFolder).FullName.Length;
                else
                    TrimLength = SourceFolder.Length;
                List<string> ar = new List<string>();
                DirectoryInfo src = new DirectoryInfo(SourceFolder);
                foreach (FileInfo file in src.GetFiles("*", SearchOption.AllDirectories))
                {
                    ar.Add(file.FullName);
                }
                if (ar.Count == 0)
                    return Result.NoRowsFound;

                FileStream ostream;
                byte[] obuffer;
                ZipOutputStream oZipStream = new ZipOutputStream(File.Create(ZipFilePath));
                oZipStream.SetLevel(level);
                oZipStream.UseZip64 = UseZip64.Off;
                ZipEntry oZipEntry;
                foreach (string Fil in ar)
                {
                    oZipEntry = new ZipEntry(Fil.Remove(0, TrimLength));
                    oZipStream.PutNextEntry(oZipEntry);
                    if (!Fil.EndsWith(@"/"))
                    {
                        ostream = File.OpenRead(Fil);
                        obuffer = new byte[ostream.Length];
                        ostream.Read(obuffer, 0, obuffer.Length);
                        oZipStream.Write(obuffer, 0, obuffer.Length);
                        ostream.Flush();
                        ostream.Close();
                        ostream.Dispose();
                    }
                }
                oZipStream.Finish();
                oZipStream.Close();
                oZipStream.Dispose();

                if (DeleteSource)
                {
                    src.Delete(true);
                }

                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public DataTable GetActiveFilesManagementJobs()
        {
            DataTable dtJobs = new DataTable();
            try
            {
                incubeQuery = new InCubeQuery("SELECT * FROM Int_FilesManagementJobs WHERE IsDeleted = 0", db_exec);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                    dtJobs = incubeQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtJobs;
        }
        public DataTable GetActiveDatabaseBackupJob()
        {
            DataTable dtJobs = new DataTable();
            try
            {
                incubeQuery = new InCubeQuery("SELECT * FROM Int_DataBaseBackupJobs WHERE IsDeleted = 0", db_exec);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                    dtJobs = incubeQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtJobs;
        }
        public DataTable GetAllFieldExecutionSummary(DateTime FromDate, DateTime ToDate)
        {
            DataTable dtSummary = new DataTable();
            try
            {
                incubeQuery = new InCubeQuery(db_exec, string.Format(@"SELECT T.FieldID,L.Value + ' ' + F.FieldName Field,MIN(T.ID) MinTriggerID,MAX(T.ID) MaxTriggerID,COUNT(*) [Count]
,MIN(T.TimeInterval) [MinSeconds],dbo.RepresentTime(MIN(T.TimeInterval),1,1,1) [Min]
,MAX(T.TimeInterval) [MaxSeconds],dbo.RepresentTime(MAX(T.TimeInterval),1,1,1) [Max]
,AVG(T.TimeInterval) [AvgSeconds],dbo.RepresentTime(AVG(T.TimeInterval),1,1,1) [Avg] 
,SUM(T.TimeInterval) [TotalSeconds],dbo.RepresentTime(SUM(T.TimeInterval),1,1,1) [Total] 
FROM (
SELECT ID,FieldID,DATEDIFF(SS,RunTimeStart,RunTimeEnd) TimeInterval FROM Int_ActionTrigger 
WHERE RunTimeEnd IS NOT NULL 
AND RunTimeStart >= '{0}'
AND RunTimeEnd <= '{1}') T
INNER JOIN Int_Field F ON F.FieldID = T.FieldID
INNER JOIN Int_Lookups L ON L.LookupName = 'ActionType' AND L.ID= F.ActionType
GROUP BY T.FieldID, F.FieldName, L.Value", FromDate.ToString("yyyy-MM-dd HH:mm:ss"), ToDate.ToString("yyyy-MM-dd HH:mm:ss")));
                incubeQuery.Execute();
                dtSummary = incubeQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtSummary;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_exec != null)
                    db_exec.Dispose();

                System.Data.SqlClient.SqlConnection.ClearAllPools();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
