using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InCubeIntegration_BL.BRF_SAP_WS;
using System.Data;
using InCubeLibrary;
using InCubeIntegration_DAL;

namespace InCubeIntegration_BL
{
    public class WarehouseManager : System.IDisposable
    {
        InCubeDatabase db;
        InCubeQuery incubeQuery;

        public WarehouseManager()
        {
            db = new InCubeDatabase();
            db.Open("InCube", "WarehouseManager");
        }

        public DataTable GetWarehouses(int OrganizationID)
        {
            DataTable dtWarehouses = new DataTable();
            try
            {
                string query = "SELECT W.WarehouseID, W.WarehouseCode + ' - ' + WL.Description Warehouse FROM Warehouse W LEFT JOIN WarehouseLanguage WL ON WL.WarehouseID = W.WarehouseID AND WL.LanguageID = 1 WHERE W.InActive = 0";

                switch (CoreGeneral.Common.GeneralConfigurations.SiteSymbol.ToLower())
                {
                    case "brf":
                        query += " AND W.WarehouseTypeID = 1 AND W.WarehouseID IN (SELECT WarehouseID FROM VehicleLoadingWh) AND W.OrganizationID = " + OrganizationID;
                        break;
                    case "ain":
                        query = "SELECT Warehouse.WarehouseID, Warehouse.Barcode Warehouse FROM Warehouse";
                        break;
                    case "maidubai":
                        query = string.Format(@"SELECT W.WarehouseID, W.WarehouseCode Warehouse
FROM Warehouse W 
INNER JOIN EmployeeVehicle EV ON EV.VehicleID = W.WarehouseID
INNER JOIN Employee E ON E.EmployeeID = EV.EmployeeID
WHERE W.WarehouseTypeID = 2");
                        if (CoreGeneral.Common.CurrentSession.EmployeeID != 0)
                            query += string.Format(" AND E.Email IN (SELECT Email FROM Employee WHERE EmployeeID = {0})", CoreGeneral.Common.CurrentSession.EmployeeID);
                        break;
                    case "qnie":
                        query = string.Format(@"SELECT WarehouseID, WarehouseCode Warehouse FROM Warehouse WHERE WarehouseTypeID = 2 AND OrganizationID = {0}", OrganizationID);
                        break;
                    case "abc":
                        query += " AND W.WarehouseTypeID = 1 AND W.WarehouseID IN (SELECT WarehouseID FROM VehicleLoadingWh)";
                        break;
                }

                incubeQuery = new InCubeQuery(query, db);
                if (incubeQuery.Execute() == InCubeErrors.Success)
                {
                    dtWarehouses = incubeQuery.GetDataTable();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return dtWarehouses;
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
