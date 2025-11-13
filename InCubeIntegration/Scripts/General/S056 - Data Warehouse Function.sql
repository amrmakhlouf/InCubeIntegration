IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'TransferTypeID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_DataTrasferGroups'))
BEGIN
	EXEC('ALTER TABLE Int_DataTrasferGroups ADD TransferTypeID INT')
END
EXEC('UPDATE Int_DataTrasferGroups SET TransferTypeID = 1 WHERE TransferTypeID IS NULL')
EXEC('ALTER TABLE Int_DataTrasferGroups ALTER COLUMN TransferTypeID INT NOT NULL')

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'TransferTypeID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_DataTransferType'))
BEGIN
	EXEC('ALTER TABLE Int_DataTransferType ADD TransferTypeID INT')
END
EXEC('UPDATE Int_DataTransferType SET TransferTypeID = 1 WHERE TransferTypeID IS NULL')
EXEC('ALTER TABLE Int_DataTransferType ALTER COLUMN TransferTypeID INT NOT NULL')

IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'TransferTypes' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('TransferTypes',1,'Data Transfer')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'TransferTypes' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('TransferTypes',2,'Data Warehouse')

IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Sequence' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_DataTransferType'))
BEGIN
	EXEC('ALTER TABLE Int_DataTransferType DROP COLUMN Sequence')
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 50)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (50,3,'Data Warehouse')
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 50 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (50,2,14)
END

IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 50)
BEGIN
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (50,16)
END