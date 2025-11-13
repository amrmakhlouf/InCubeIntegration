IF (COLUMNPROPERTY(OBJECT_ID('Int_MailSenderProfile'),'SenderProfileID','IsIdentity') = 1)
BEGIN
	EXEC('ALTER TABLE Int_MailSenderProfile ADD SenderProfileID2 INT')
	EXEC('UPDATE Int_MailSenderProfile SET SenderProfileID2 = SenderProfileID')
	EXEC('IF EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_MailSenderProfile'')
		ALTER TABLE Int_MailSenderProfile DROP CONSTRAINT PK_Int_MailSenderProfile')
	EXEC('ALTER TABLE Int_MailSenderProfile DROP COLUMN SenderProfileID')
	EXEC sp_rename 'Int_MailSenderProfile.SenderProfileID2', 'SenderProfileID', 'COLUMN';
	EXEC('ALTER TABLE Int_MailSenderProfile ALTER COLUMN SenderProfileID INT NOT NULL')
	EXEC('ALTER TABLE Int_MailSenderProfile ADD CONSTRAINT [PK_Int_MailSenderProfile] PRIMARY KEY CLUSTERED (SenderProfileID ASC)')
END

IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',4,'ExcelExport')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 5)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',5,'DataTransfer')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 6)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',6,'OracleProcedure')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'PrivilegeType' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('PrivilegeType',4,'MenuAction')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',1,'Second')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',2,'Minute')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 3)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',3,'Hour')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',4,'Day')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 5)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',5,'Month')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'AgeTimeUnit' AND ID = 6)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('AgeTimeUnit',6,'Year')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ComparisonOperator' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ComparisonOperator',1,'EqualTo')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ComparisonOperator' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ComparisonOperator',2,'GreaterThan')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ComparisonOperator' AND ID = 3)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ComparisonOperator',3,'GreaterThanOrEqualTo')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ComparisonOperator' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ComparisonOperator',4,'LessThan')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ComparisonOperator' AND ID = 5)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ComparisonOperator',5,'LessThanOrEqualTo')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ServiceStatus' AND ID = 0)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ServiceStatus',0,'UnKnown')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ServiceStatus' AND ID = 1)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ServiceStatus',1,'NotInstalled')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ServiceStatus' AND ID = 2)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ServiceStatus',2,'Running')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ServiceStatus' AND ID = 3)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ServiceStatus',3,'Stopped')
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ServiceStatus' AND ID = 4)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ServiceStatus',4,'Disabled')
	
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_MailAttachments')
BEGIN
	CREATE TABLE Int_MailAttachments
	(
	TriggerID INT,
	MailNo INT,
	AttachmentID INT,
	AttachmentType INT,
	QueryString NVARCHAR(MAX),
	FileName NVARCHAR(50),
	AttachmentPath NVARCHAR(400),
	AttachmentName NVARCHAR(200),
	RowsFound INT,
	CONSTRAINT [PK_Int_MailAttachments] PRIMARY KEY CLUSTERED 
	(
	TriggerID ASC,
	MailNo ASC,
	AttachmentID ASC
	)) ON [PRIMARY];
END

ALTER TABLE Int_PreparedMails ALTER COLUMN SendingTime DATETIME NULL