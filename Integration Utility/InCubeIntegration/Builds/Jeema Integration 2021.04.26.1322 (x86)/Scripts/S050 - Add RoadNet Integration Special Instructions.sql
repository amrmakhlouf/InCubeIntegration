IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 20 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (20,1,'RaodNet Special Instructions',16)
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'RoadNetSpecialInstructions')
BEGIN
	CREATE TABLE RoadNetSpecialInstructions
	(
	CustomerID INT,
	OutletID INT,
	SpecialInstructions NVARCHAR(400),
	CONSTRAINT [PK_RoadNetSpecialInstructions] PRIMARY KEY CLUSTERED 
	(
	[CustomerID] ASC,
	[OutletID] ASC
	)) ON [PRIMARY];
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Stg_SpecialInstructions')
BEGIN
CREATE TABLE [dbo].[Stg_SpecialInstructions](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerCode] [nvarchar](200) NULL,
	[OutletCode] [nvarchar](200) NULL,
	[SpecialInstructions] [nvarchar](200) NULL,
	[ResultID] [int] NULL,
	[Inserted] [bit] NULL,
	[Updated] [bit] NULL,
	[Skipped] [bit] NULL,
	[Message] [nvarchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateRoadNetSpecialInstructions')
BEGIN
EXEC('
CREATE PROCEDURE sp_UpdateRoadNetSpecialInstructions
AS
BEGIN

UPDATE SI SET Message = CASE 
WHEN C.CustomerID IS NULL THEN ''Invalid customer code''
WHEN CO.OutletID IS NULL THEN ''Invalid outlet code''
WHEN R.CustomerID IS NOT NULL THEN ''Updated''
ELSE ''Inserted'' END
FROM Stg_SpecialInstructions SI
LEFT JOIN Customer C ON C.CustomerCode = SI.CustomerCode
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletCode AND CO.CustomerID = C.CustomerID
LEFT JOIN RoadNetSpecialInstructions R ON R.CustomerID = C.CustomerID AND R.OutletID = CO.OutletID

UPDATE Stg_SpecialInstructions SET ResultID = 1, Inserted = 1, Updated = 0, Skipped = 0 WHERE Message = ''Inserted''
UPDATE Stg_SpecialInstructions SET ResultID = 1, Inserted = 0, Updated = 1, Skipped = 0 WHERE Message = ''Updated''
UPDATE Stg_SpecialInstructions SET ResultID = 2, Inserted = 0, Updated = 0, Skipped = 1 WHERE ResultID IS NULL

INSERT INTO RoadNetSpecialInstructions (CustomerID,OutletID,SpecialInstructions)
SELECT C.CustomerID,CO.OutletID,SI.SpecialInstructions
FROM Stg_SpecialInstructions SI
INNER JOIN Customer C ON C.CustomerCode = SI.CustomerCode
INNER JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletCode AND CO.CustomerID = C.CustomerID
WHERE SI.Inserted = 1

UPDATE SI SET SpecialInstructions = S.SpecialInstructions
FROM RoadNetSpecialInstructions SI
INNER JOIN Customer C ON C.CustomerCode = SI.CustomerID
INNER JOIN CustomerOutlet CO ON CO.CustomerCode = SI.OutletID AND CO.CustomerID = C.CustomerID
INNER JOIN Stg_SpecialInstructions S ON S.CustomerCode = C.CustomerCode AND S.OutletCode = CO.CustomerCode
WHERE S.Updated = 1

END
')
END

DECLARE @ImportTypeID INT = -1;
SELECT @ImportTypeID = ImportTypeID FROM Int_ExcelImportTypes WHERE Name = 'RoadNet Special Instructions';

IF (@ImportTypeID <> -1)
BEGIN
	DELETE FROM Int_ExcelImportTypes WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportSheets WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportColumns WHERE ImportTypeID = @ImportTypeID;
	DELETE FROM Int_ExcelImportProcedures WHERE ImportTypeID = @ImportTypeID;
END
ELSE BEGIN
	SELECT @ImportTypeID = ISNULL(MAX(ImportTypeID),0)+1 FROM Int_ExcelImportTypes;
END

--Int_ExcelImportTypes
INSERT INTO Int_ExcelImportTypes (ImportTypeID,Name,Description) VALUES (@ImportTypeID,'RoadNet Special Instructions','This imports RoadNet Special Instructions by adding new instructions and update existing');

--Int_ExcelImportSheets
INSERT INTO Int_ExcelImportSheets (ImportTypeID,SheetNo,SheetDescription,StagingTable) VALUES (@ImportTypeID,1,'Special Instructions','Stg_SpecialInstructions');

--Int_ExcelImportColumns
INSERT INTO Int_ExcelImportColumns (ImportTypeID,SheetNo,Sequence,FieldName,FieldType) VALUES (@ImportTypeID,1,1,'CustomerCode',1);
INSERT INTO Int_ExcelImportColumns (ImportTypeID,SheetNo,Sequence,FieldName,FieldType) VALUES (@ImportTypeID,1,2,'OutletCode',1);
INSERT INTO Int_ExcelImportColumns (ImportTypeID,SheetNo,Sequence,FieldName,FieldType) VALUES (@ImportTypeID,1,3,'SpecialInstructions',1);

--Int_ExcelImportProcedures
INSERT INTO Int_ExcelImportProcedures (ImportTypeID,Sequence,ProcedureName) VALUES (@ImportTypeID,1,'sp_UpdateRoadNetSpecialInstructions');

--Int_Privileges
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = @ImportTypeID AND PrivilegeType = 3)
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (@ImportTypeID,3,11);