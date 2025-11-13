IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_DatabaseConnection')
BEGIN
	CREATE TABLE Int_DatabaseConnection
	(
	ID INT NOT NULL,
	Name NVARCHAR(100) NOT NULL,
	DatabaseTypeID INT NOT NULL,
	ConnectionString NVARCHAR(max)
	)
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_DataTransferType')
BEGIN
	CREATE TABLE Int_DataTransferType
	(
	ID INT NOT NULL,
	Name NVARCHAR(100) NOT NULL,
	SelectQuery NVARCHAR(max) NOT NULL,
	SourceDatabaseID INT NOT NULL,
	DestinationDatabaseID INT NOT NULL,
	DestinationTable NVARCHAR(50) NOT NULL,
	TransferMethodID INT NOT NULL,
	PrimaryKeyColumns NVARCHAR(max) NOT NULL,
	IsDeleted BIT NOT NULL,
	[Sequence] INT NOT NULL
	)
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_DataTransferMethod')
BEGIN
	CREATE TABLE Int_DataTransferMethod
	(
	ID INT NOT NULL,
	Description NVARCHAR(100) NOT NULL
	)
END

EXEC('
IF NOT EXISTS (SELECT * FROM Int_DataTransferMethod)
BEGIN
	INSERT INTO Int_DataTransferMethod VALUES (1,''Delete all then insert''), (2,''Insert new, update existing''), (3,''Insert new only''), (4,''Update existing only'')
END
')

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 43 AND ActionType = 2)
	INSERT INTO Int_Field VALUES (43,2,'Data Transfer');
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 43 AND PrivilegeType = 2)
	INSERT INTO Int_Privileges VALUES (43,2,NULL,8);
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 43)
	INSERT INTO Int_FieldFilters VALUES (43,10);