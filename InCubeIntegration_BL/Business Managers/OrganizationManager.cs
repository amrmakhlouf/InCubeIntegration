using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeIntegration_DAL;
using InCubeLibrary;
using System.Data;

namespace InCubeIntegration_BL
{
    public class OrganizationManager : IDisposable
    {
        InCubeDatabase db_Org;
        InCubeQuery incubeQuery;

        public OrganizationManager()
        {
            db_Org = new InCubeDatabase();
            db_Org.Open("InCube", "OrganizationManager");
        }

        public DataTable GetOrganizations()
        {
            DataTable dtOrg = new DataTable();
            try
            {
                incubeQuery = new InCubeQuery(db_Org, string.Format(@"SELECT O.OrganizationID,OrganizationCode,Description FROM Organization O 
INNER JOIN OrganizationLanguage OL ON OL.OrganizationID = O.OrganizationID 
WHERE O.OrganizationID IN ({0}) AND OL.LanguageID = 1", CoreGeneral.Common.userPrivileges.Organizations));
                if (incubeQuery.Execute() == InCubeErrors.Success)
                    dtOrg = incubeQuery.GetDataTable();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtOrg;
        }
        public Result GetAllOrganizations(ref DataTable dtOrganizations)
        {
            Result res = Result.Failure;
            try
            {
                string qry = string.Format(@"SELECT OrganizationID, Description
FROM OrganizationLanguage
WHERE LanguageID = 1 AND OrganizationID IN ({0})", CoreGeneral.Common.userPrivileges.Organizations);

                incubeQuery = new InCubeQuery(db_Org, qry);

                dtOrganizations = new DataTable();
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtOrganizations = incubeQuery.GetDataTable();
                    if (dtOrganizations.Rows.Count > 0)
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
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db_Org != null)
                    db_Org.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
