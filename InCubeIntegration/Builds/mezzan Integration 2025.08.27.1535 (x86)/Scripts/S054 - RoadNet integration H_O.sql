UPDATE Int_Privileges SET Description = 'RaodNet Standard Instructions' WHERE PrivilegeID = 20 AND PrivilegeType = 1

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RoadNetSpecialInstructions')
BEGIN
	EXEC sp_rename 'RoadNetSpecialInstructions', 'RoadNetStandardInstructions'
END

IF EXISTS (SELECT * FROM sys.columns WHERE name = 'SpecialInstructions' AND OBJECT_ID = OBJECT_ID('RoadNetStandardInstructions'))
BEGIN
	EXEC sp_rename 'RoadNetStandardInstructions.SpecialInstructions' , 'StandardInstructions', 'COLUMN'
END

DECLARE @ImportTypeID INT = -1;
SELECT @ImportTypeID = ImportTypeID FROM Int_ExcelImportTypes WHERE Name = 'RoadNet Special Instructions'
IF (@ImportTypeID <> -1)
BEGIN
	UPDATE Int_ExcelImportTypes SET Name = REPLACE(Name,'Special','Standard'), Description = REPLACE(Description,'Special','Standard') 
	WHERE ImportTypeID = @ImportTypeID;
	UPDATE Int_ExcelImportSheets SET SheetDescription = REPLACE(SheetDescription,'Special','Standard'), StagingTable = REPLACE(StagingTable,'Special','Standard') 
	WHERE ImportTypeID = @ImportTypeID;
	UPDATE Int_ExcelImportColumns SET FieldName = 'StandardInstructions' WHERE FieldName = 'SpecialInstructions' AND ImportTypeID = @ImportTypeID;
	UPDATE Int_ExcelImportProcedures SET ProcedureName = 'sp_UpdateRoadNetStandardInstructions' WHERE ImportTypeID = @ImportTypeID;
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateRoadNetStandardInstructions')
BEGIN
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateRoadNetStandardInstructions]
AS
BEGIN

UPDATE SI SET Message = CASE 
WHEN C.CustomerID IS NULL THEN ''Invalid customer code''
WHEN CO.OutletID IS NULL THEN ''Invalid outlet code''
WHEN R.CustomerID IS NOT NULL THEN ''Updated''
ELSE ''Inserted'' END
FROM Stg_StandardInstructions SI
LEFT JOIN Customer C ON C.CustomerCode = SI.CustomerCode
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletCode AND CO.CustomerID = C.CustomerID
LEFT JOIN RoadNetStandardInstructions R ON R.CustomerID = C.CustomerID AND R.OutletID = CO.OutletID

UPDATE Stg_StandardInstructions SET ResultID = 1, Inserted = 1, Updated = 0, Skipped = 0 WHERE Message = ''Inserted''
UPDATE Stg_StandardInstructions SET ResultID = 1, Inserted = 0, Updated = 1, Skipped = 0 WHERE Message = ''Updated''
UPDATE Stg_StandardInstructions SET ResultID = 2, Inserted = 0, Updated = 0, Skipped = 1 WHERE ResultID IS NULL

INSERT INTO RoadNetStandardInstructions (CustomerID,OutletID,StandardInstructions)
SELECT C.CustomerID,CO.OutletID,SI.StandardInstructions
FROM Stg_StandardInstructions SI
INNER JOIN Customer C ON C.CustomerCode = SI.CustomerCode
INNER JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletCode AND CO.CustomerID = C.CustomerID
WHERE SI.Inserted = 1

UPDATE SI SET StandardInstructions = S.StandardInstructions
FROM RoadNetStandardInstructions SI
INNER JOIN Customer C ON C.CustomerCode = SI.CustomerID
INNER JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletID AND CO.CustomerID = C.CustomerID
INNER JOIN Stg_StandardInstructions S ON S.CustomerCode = C.CustomerCode AND S.OutletCode = CO.CustomerCode
WHERE S.Updated = 1

END
')
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 21 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (21,1,'PRN Configuraion',16)
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_PRNConfig')
BEGIN
	CREATE TABLE Int_PRNConfig
	(
	ColumnName NVARCHAR(50),
	Position INT,
	Width INT,
	CONSTRAINT [PK_Int_PRNConfig] PRIMARY KEY CLUSTERED 
	(
	[ColumnName] ASC
	)) ON [PRIMARY];
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 1 AND PrivilegeType = 4)
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (1,4,'Home and Office',17)
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 2 AND PrivilegeType = 4)
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (2,4,'Delivery',17)

INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence,DefaultCheck)
SELECT UserID,2,4,1,0 FROM (
SELECT UserID FROM Int_UserPrivileges WHERE PrivilegeType = 1 AND PrivilegeID = 17
EXCEPT
SELECT UserID FROM Int_UserPrivileges WHERE PrivilegeType = 4 AND PrivilegeID = 2) T

IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'PrivilegeType' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('PrivilegeType',4,'MenuAction')