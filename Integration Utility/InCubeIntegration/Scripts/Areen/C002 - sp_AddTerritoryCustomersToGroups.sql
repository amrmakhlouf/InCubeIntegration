IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_AddTerritoryCustomersToGroups')
BEGIN

EXEC('
CREATE PROCEDURE sp_AddTerritoryCustomersToGroups AS
BEGIN

INSERT INTO CustomerGroup
SELECT RANK() OVER(ORDER BY EmployeeCode) + (SELECT MAX(GroupID) FROM CustomerGroup) GroupID
, E.EmployeeCode GroupCode, NULL, NULL
FROM EmployeeTerritory ET
INNER JOIN Employee E ON E.EmployeeID = ET.EmployeeID
LEFT JOIN CustomerGroup CG ON CG.GroupCode = E.EmployeeCode
WHERE CG.GroupCode IS NULL AND E.EmployeeTypeID = 2 AND ISNULL(E.EmployeeID,'''') <> '''' 

INSERT INTO CustomerGroupLanguage
SELECT CG.GroupID,1,E.EmployeeCode + '' - '' + EL.Description
FROM CustomerGroup CG
LEFT JOIN CustomerGroupLanguage CGL ON CGL.GroupID = CG.GroupID
INNER JOIN Employee E ON E.EmployeeCode = CG.GroupCode
INNER JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID AND EL.LanguageID = 1
WHERE CGL.GroupID IS NULL

DELETE FROM CustomerOutletGroup WHERE GroupID IN (SELECT GroupID FROM CustomerGroup WHERE GroupCode IN (SELECT EmployeeCode FROM Employee))

INSERT INTO CustomerOutletGroup
SELECT COT.CustomerID, COT.OutletID, CG.GroupID
FROM CustomerGroup CG
INNER JOIN Employee E ON E.EmployeeCode = CG.GroupCode
INNER JOIN EmployeeTerritory ET ON ET.EmployeeID = E.EmployeeID
INNER JOIN CustOutTerritory COT ON COT.TerritoryID = ET.TerritoryID

END')

END