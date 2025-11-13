--Add schema for grouping data transfer types
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'Int_DataTrasferGroups')
BEGIN
	CREATE TABLE Int_DataTrasferGroups
	(
	GroupID INT NOT NULL,
	GroupName NVARCHAR(50) NOT NULL,
	 CONSTRAINT [PK_Int_DataTrasferGroups] PRIMARY KEY CLUSTERED 
	(
		[GroupID] ASC
	)) ON [PRIMARY];
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'Int_DataTrasferGroupDetails')
BEGIN
	CREATE TABLE Int_DataTrasferGroupDetails
	(
	GroupID INT NOT NULL,
	TransferTypeID INT NOT NULL,
	[Sequence] INT NOT NULL,
	 CONSTRAINT [PK_Int_DataTrasferGroupDetails] PRIMARY KEY CLUSTERED 
	(
		[GroupID] ASC,
		[TransferTypeID] ASC
	)) ON [PRIMARY];
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'DataTransferGroupID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FieldProcedure'))
BEGIN
	EXEC('ALTER TABLE Int_FieldProcedure ADD DataTransferGroupID INT')
END

--Add missing fields
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 46 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (46,2,7)
END
IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 46)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (46,1,'Serial Stock')
END
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 47 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (47,2,7)
END
IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 47)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (47,1,'Areas')
END

--Add missing lookups
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'LoginType' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('LoginType',1,'WindowsService');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'LoginType' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('LoginType',2,'User');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'LoginType' AND ID = 3)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('LoginType',3,'NoLoginForm');

IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',4,'ExcelExport');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 5)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',5,'DataTransfer');

IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',1,'Second');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',2,'Minute');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 3)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',3,'Hour');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',4,'Day');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 5)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',5,'Month');
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 6)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',6,'Year');

