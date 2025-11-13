IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_Lookups')
BEGIN

	CREATE TABLE Int_Lookups
	(
	LookupName NVARCHAR(50) NOT NULL,
	ID INT NOT NULL,
	Value nvarchar(50) NOT NULL,
	 CONSTRAINT [PK_Int_Lookups] PRIMARY KEY CLUSTERED 
	(
		LookupName ASC,
		ID ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	)

END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_DataTransferMethod')
BEGIN
	DROP TABLE Int_DataTransferMethod
END

EXEC('
DELETE FROM Int_Lookups;

--ActionType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ActionType'',1,''Update'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ActionType'',2,''Send'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ActionType'',3,''SpecialFunctions'');
--Result
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',0,''UnKnown'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',1,''Success'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',2,''Failure'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',3,''NoRowsFound'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',4,''Invalid'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',5,''InActive'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',6,''LoggedIn'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',7,''WebServiceConnectionError'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',8,''NoFileRetreived'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',9,''Duplicate'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',10,''ErrorExecutingQuery'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',11,''Started'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',12,''NotInitialized'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Result'',13,''Blocked'');
--ConfigurationType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ConfigurationType'',1,''String'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ConfigurationType'',2,''Boolean'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ConfigurationType'',3,''Long'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ConfigurationType'',4,''Color'');
--ParamType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ParamType'',1,''Integer'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ParamType'',2,''Nvarchar'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ParamType'',3,''DateTime'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ParamType'',4,''BIT'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ParamType'',5,''Decimal'');
--ProcType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ProcType'',1,''SQLProcedure'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ProcType'',2,''SMS'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ProcType'',3,''Mail'');
--ColumnType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',0,''Int'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',1,''String'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',2,''Decimal'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',3,''Datetime'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',4,''Bool'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ColumnType'',5,''Image'');
--DataBaseType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''DataBaseType'',1,''SQLServer'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''DataBaseType'',2,''Oracle'');
--TaskStatus
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TaskStatus'',1,''Active'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TaskStatus'',2,''Stopped'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TaskStatus'',3,''Deleted'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TaskStatus'',4,''Changed'');
--ScheduleType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ScheduleType'',1,''DailyEvery'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ScheduleType'',2,''DailyAt'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ScheduleType'',3,''Weekly'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ScheduleType'',4,''Monthly'');
--Priority
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Priority'',1,''High'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Priority'',2,''Medium'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Priority'',3,''Low'');
--PrivilegeType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''PrivilegeType'',1,''MenuAccess'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''PrivilegeType'',2,''FieldAccess'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''PrivilegeType'',3,''ExcelImport'');
--MailRecipientType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''MailRecipientType'',1,''To'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''MailRecipientType'',2,''CC'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''MailRecipientType'',3,''BCC'');
--FileJobType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''FileJobType'',1,''Delete'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''FileJobType'',2,''Move'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''FileJobType'',3,''Copy'');
--Queues_Mode
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Queues_Mode'',1,''Org_Action'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''Queues_Mode'',2,''TaskID'');
--TransferMethod
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TransferMethod'',1,''Delete all then insert'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TransferMethod'',2,''Insert new, update existing'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TransferMethod'',3,''Insert new only'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''TransferMethod'',4,''Update existing only'');
')
