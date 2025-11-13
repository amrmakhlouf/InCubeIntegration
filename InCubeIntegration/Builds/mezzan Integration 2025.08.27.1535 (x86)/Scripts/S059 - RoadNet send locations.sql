IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 22 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (22,1,'Locations Export',16)
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 23 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (23,1,'Journey Plan Import',16)
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetCustomerGroupsForUser')
BEGIN
EXEC('
CREATE PROCEDURE sp_GetCustomerGroupsForUser
(
@EmployeeID INT
)
AS
BEGIN

SELECT DISTINCT CG.GroupID, CGL.Description
FROM CustomerGroup CG
INNER JOIN CustomerGroupLanguage CGL ON CGL.GroupID = CG.GroupID AND CGL.LanguageID = 1
INNER JOIN EmployeeOrganization EO ON EO.OrganizationID = CG.OrganizationID AND EO.EmployeeID = @EmployeeID

END
')
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetLocationsForUser')
BEGIN
EXEC('
CREATE PROCEDURE [dbo].[sp_GetLocationsForUser]
(
@EmployeeID INT,
@GroupID INT
)
AS
BEGIN

SELECT DISTINCT CO.CustomerCode, CO.Phone PhoneNo, CO.OpenTime, CO.CloseTime, CO.TW1Start, CO.TW1Stop, CO.TW2Start, CO.TW2Stop, CO.FixedServiceTime, CO.VariableServiceTime * 10 / 60 VariableServiceTime
, COL.Description CustomerName, 100 DropSize, CO.PostalCode VisitPatternSet, ''A'' AccountType, COl.Address Address1, COL.Address2, CO.GPSLatitude Latitude, CO.GPSLongitude Longitude
, O.OrganizationCode RegionID
FROM CustomerOutlet CO
INNER JOIN CustomerOutletLanguage COL ON COL.CustomerID = CO.CustomerID AND COL.OutletID = CO.OutletID AND COL.LanguageID = 1
INNER JOIN EmployeeOrganization EO ON EO.OrganizationID = CO.OrganizationID AND EO.EmployeeID = @EmployeeID
LEFT JOIN CustomerOutletGroup COG ON COG.CustomerID = CO.CustomerID AND COG.OutletID = CO.OutletID
INNER JOIN Organization O ON O.OrganizationID = CO.OrganizationID
WHERE (@GroupID = -1 OR COG.GroupID = @GroupID)

END

')
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_AddExtraColumnsToJourneyPlan')
BEGIN
EXEC('
CREATE PROCEDURE sp_AddExtraColumnsToJourneyPlan
AS
BEGIN
	EXEC(''ALTER TABLE Stg_JourneyPlan ADD CustomerID INT'')
	EXEC(''ALTER TABLE Stg_JourneyPlan ADD OutletID INT'')
	EXEC(''ALTER TABLE Stg_JourneyPlan ADD TerritoryID INT'')
	EXEC(''ALTER TABLE Stg_JourneyPlan ADD RouteID INT'')
END
')
END