IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_UpdateCustomersForSupervisors')
BEGIN

EXEC('
CREATE PROCEDURE sp_UpdateCustomersForSupervisors AS

DELETE FROM CustOutTerritory WHERE TerritoryID IN (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID IN (SELECT SupervisorID FROM EmployeeSupervisor))

INSERT INTO CustOutTerritory (CustomerID,OutletID,TerritoryID)
SELECT DISTINCT COT.CustomerID, COT.OutletID, ST.TerritoryID
FROM EmployeeSupervisor ES
INNER JOIN EmployeeTerritory ST ON ST.EmployeeID = ES.SupervisorID
INNER JOIN EmployeeTerritory ET ON ET.EmployeeID = ES.EmployeeID
INNER JOIN CustOutTerritory COT ON COT.TerritoryID = ET.TerritoryID

DELETE FROM RouteCustomer WHERE RouteID IN (SELECT RouteID FROM Route WHERE TerritoryID IN (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID IN (SELECT SupervisorID FROM EmployeeSupervisor)))

INSERT INTO RouteCustomer (RouteID,CustomerID,OutletID,Sequence)
SELECT DISTINCT SR.RouteID,RC.CustomerID,RC.OutletID,RC.Sequence
FROM EmployeeSupervisor ES
INNER JOIN EmployeeTerritory ST ON ST.EmployeeID = ES.SupervisorID
INNER JOIN EmployeeTerritory ET ON ET.EmployeeID = ES.EmployeeID
INNER JOIN Route SR ON SR.TerritoryID = ST.TerritoryID 
INNER JOIN Route R ON R.TerritoryID = ET.TerritoryID
INNER JOIN CustOutTerritory COT ON COT.TerritoryID = ET.TerritoryID
INNER JOIN RouteCustomer RC ON RC.RouteID = R.RouteID')

END