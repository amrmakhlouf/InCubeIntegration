using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Data;

namespace InCubeIntegration_BL
{
    public class EmployeeManager : System.IDisposable
    {
        InCubeDatabase db;
        InCubeQuery incubeQuery;
        public EmployeeManager()
        {
            db = new InCubeDatabase();
            db.Open("InCube", "EmployeeManager");
        }

        public DataTable GetEmployees(int OrganizationID)
        {
            DataTable dtEmployees = new DataTable();
            try
            {
                string query = "SELECT E.EmployeeID, E.EmployeeCode + ' - ' + EL.Description Employee FROM Employee E LEFT JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID AND EL.LanguageID = 1 WHERE EmployeeTypeID in(2,3,6,10) AND InActive = 0";// Salesman,Deleviry,Collector,Presales

                switch (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower())
                {
                    case "brf":
                        query = @"SELECT E.EmployeeID, ISNULL(E.Email,'') + CASE ISNULL(E.Email,'') WHEN '' THEN '' ELSE ' - ' END + ISNULL(EL.Description,'') Employee 
                                  FROM Employee E INNER JOIN EmployeeLanguage EL ON E.EmployeeID = EL.EmployeeID AND EL.LanguageID = 1
                                  WHERE E.EmployeeTypeID = 2 AND E.InActive = 0 AND E.OrganizationID = " + OrganizationID +
                              " ORDER BY CASE ISNULL(E.Email,'') WHEN '' THEN 'ZZZ' ELSE E.Email END";
                        break;
                    case "maidubai":
                        if (CoreGeneral.Common.CurrentSession.EmployeeID != 0)
                            query += string.Format(" AND E.Email IN (SELECT Email FROM Employee WHERE EmployeeID = {0})", CoreGeneral.Common.CurrentSession.EmployeeID);
                        break;
                }

                incubeQuery = new InCubeQuery(db, query);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtEmployees = incubeQuery.GetDataTable();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtEmployees;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db != null)
                    db.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}
