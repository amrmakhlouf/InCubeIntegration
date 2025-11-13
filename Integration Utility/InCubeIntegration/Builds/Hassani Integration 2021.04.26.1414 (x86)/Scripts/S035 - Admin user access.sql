UPDATE Int_Privileges SET PrivilegeType = 2 WHERE PrivilegeID = 32
UPDATE Int_Privileges SET ParentID = 14 WHERE PrivilegeID IN (33,41)
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 14 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (14,1,'Integration Special Actions',1)
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Privileges')
	ALTER TABLE Int_Privileges ADD CONSTRAINT [PK_Int_Privileges] PRIMARY KEY CLUSTERED (PrivilegeType ASC, PrivilegeID ASC)

UPDATE Int_UserPrivileges SET Sequence = 0 WHERE Sequence IS NULL
ALTER TABLE Int_UserPrivileges ALTER COLUMN Sequence INT NOT NULL
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_UserPrivileges')
	ALTER TABLE Int_UserPrivileges ADD CONSTRAINT [PK_Int_UserPrivileges] PRIMARY KEY CLUSTERED (UserID ASC, PrivilegeType ASC, PrivilegeID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_ActionFilter')
	ALTER TABLE Int_ActionFilter ADD CONSTRAINT [PK_Int_ActionFilter] PRIMARY KEY CLUSTERED (TaskID ASC, ActionID ASC, FilterID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_ActionTrigger')
	ALTER TABLE Int_ActionTrigger ADD CONSTRAINT [PK_Int_ActionTrigger] PRIMARY KEY CLUSTERED (ID ASC)
UPDATE Int_Configuration SET ReadOnly = 0 WHERE ReadOnly IS NULL
ALTER TABLE Int_Configuration ALTER COLUMN ReadOnly BIT NOT NULL
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaInt_Configuration_PK')
	ALTER TABLE Int_Configuration DROP CONSTRAINT [aaaaaInt_Configuration_PK]
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Configuration')
	ALTER TABLE Int_Configuration ADD CONSTRAINT [PK_Int_Configuration] PRIMARY KEY CLUSTERED (ConfigurationID ASC, OrganizationID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_DatabaseConnection')
	ALTER TABLE Int_DatabaseConnection ADD CONSTRAINT [PK_Int_DatabaseConnection] PRIMARY KEY CLUSTERED (ID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_DataTransferMethod')
	ALTER TABLE Int_DataTransferMethod ADD CONSTRAINT [PK_Int_DataTransferMethod] PRIMARY KEY CLUSTERED (ID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_DataTransferType')
	ALTER TABLE Int_DataTransferType ADD CONSTRAINT [PK_Int_DataTransferType] PRIMARY KEY CLUSTERED (ID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_ExecutionDetails')
	ALTER TABLE Int_ExecutionDetails ADD CONSTRAINT [PK_Int_ExecutionDetails] PRIMARY KEY CLUSTERED (ID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Field')
	ALTER TABLE Int_Field ADD CONSTRAINT [PK_Int_Field] PRIMARY KEY CLUSTERED (FieldID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_FieldFilters')
	ALTER TABLE Int_FieldFilters ADD CONSTRAINT [PK_Int_FieldFilters] PRIMARY KEY CLUSTERED (FieldID ASC, FilterID ASC)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaInt_FieldProcedure_PK')
	ALTER TABLE Int_FieldProcedure DROP CONSTRAINT [aaaaaInt_FieldProcedure_PK]
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_FieldProcedure')
	ALTER TABLE Int_FieldProcedure ADD CONSTRAINT [PK_Int_FieldProcedure] PRIMARY KEY CLUSTERED (FieldID ASC, Sequence ASC)
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaInt_FieldProcParams_PK')
	ALTER TABLE Int_FieldProcParams DROP CONSTRAINT [aaaaaInt_FieldProcParams_PK]
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_FieldProcParams')
	ALTER TABLE Int_FieldProcParams ADD CONSTRAINT [PK_Int_FieldProcParams] PRIMARY KEY CLUSTERED (FieldID ASC, Sequence ASC, ParameterName ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Filter')
	ALTER TABLE Int_Filter ADD CONSTRAINT [PK_Int_Filter] PRIMARY KEY CLUSTERED (FilterID ASC)
ALTER TABLE Int_MailBody ALTER COLUMN TriggerID INT NOT NULL
ALTER TABLE Int_MailBody ALTER COLUMN MailNo INT NOT NULL
ALTER TABLE Int_MailBody ALTER COLUMN [LineNo] INT NOT NULL
ALTER TABLE Int_MailBody ALTER COLUMN LineText nvarchar(max) NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_MailBody'')
	ALTER TABLE Int_MailBody ADD CONSTRAINT [PK_Int_MailBody] PRIMARY KEY CLUSTERED (TriggerID ASC, MailNo ASC, [LineNo] ASC)')
ALTER TABLE Int_MailSenderProfile ALTER COLUMN SenderProfileID INT NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN ProfileName nvarchar(100) NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN Host nvarchar(100) NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN Port INT NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN MailAddress nvarchar(100) NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN DisplayName nvarchar(100) NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN Password nvarchar(400) NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN EnableSSL BIT NOT NULL
ALTER TABLE Int_MailSenderProfile ALTER COLUMN IsDeleted BIT NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_MailSenderProfile'')
	ALTER TABLE Int_MailSenderProfile ADD CONSTRAINT [PK_Int_MailSenderProfile] PRIMARY KEY CLUSTERED (SenderProfileID ASC)')
ALTER TABLE Int_MailTemplate ALTER COLUMN MailTemplateID INT NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN TemplateName nvarchar(100) NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN SenderProfileID INT NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN Subject nvarchar(max) NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN Header nvarchar(max) NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN Footer nvarchar(max) NOT NULL
ALTER TABLE Int_MailTemplate ALTER COLUMN IsDeleted BIT NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_MailTemplate'')
	ALTER TABLE Int_MailTemplate ADD CONSTRAINT [PK_Int_MailTemplate] PRIMARY KEY CLUSTERED (MailTemplateID ASC)')
ALTER TABLE Int_MailTemplateRecipients ALTER COLUMN MailTemplateID INT NOT NULL
ALTER TABLE Int_MailTemplateRecipients ALTER COLUMN RecipientAddress nvarchar(50) NOT NULL
ALTER TABLE Int_MailTemplateRecipients ALTER COLUMN RecipientType INT NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_MailTemplateRecipients'')
	ALTER TABLE Int_MailTemplateRecipients ADD CONSTRAINT [PK_Int_MailTemplateRecipients] PRIMARY KEY CLUSTERED (MailTemplateID ASC, RecipientAddress ASC)')
ALTER TABLE Int_PreparedMails ALTER COLUMN TriggerID INT NOT NULL
ALTER TABLE Int_PreparedMails ALTER COLUMN MailNo INT NOT NULL
ALTER TABLE Int_PreparedMails ALTER COLUMN SendingTime DATETIME NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_PreparedMails'')
	ALTER TABLE Int_PreparedMails ADD CONSTRAINT [PK_Int_PreparedMails] PRIMARY KEY CLUSTERED (TriggerID ASC, MailNo ASC)')
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Session')
	ALTER TABLE Int_Session ADD CONSTRAINT [PK_Int_Session] PRIMARY KEY CLUSTERED (SessionID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_TaskAction')
	ALTER TABLE Int_TaskAction ADD CONSTRAINT [PK_Int_TaskAction] PRIMARY KEY CLUSTERED (TaskID ASC, FieldID ASC)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'PK_Int_Tasks')
	ALTER TABLE Int_Tasks ADD CONSTRAINT [PK_Int_Tasks] PRIMARY KEY CLUSTERED (TaskID ASC)
UPDATE Int_TaskSchedule SET EndTime = NULL WHERE EndTime = 'null'
UPDATE Int_TaskSchedule SET [Day] = -1 WHERE [Day] IS NULL
ALTER TABLE Int_TaskSchedule ALTER COLUMN [Time] NVARCHAR(4) NOT NULL
ALTER TABLE Int_TaskSchedule ALTER COLUMN EndTime NVARCHAR(4) NULL
ALTER TABLE Int_TaskSchedule ALTER COLUMN [Day] INT NOT NULL
EXEC('IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = ''PK_Int_TaskSchedule'')
	ALTER TABLE Int_TaskSchedule ADD CONSTRAINT [PK_Int_TaskSchedule] PRIMARY KEY CLUSTERED (TaskID ASC, ScheduleType ASC, [Time] ASC, [Day] ASC)')
/*
INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID)
SELECT ImportTypeID,3,11 FROM ImportTypes
EXCEPT
SELECT PrivilegeID,PrivilegeType,ParentID FROM Int_Privileges WHERE ParentID = 11
*/
UPDATE UP SET Sequence = SEQ FROM Int_UserPrivileges UP INNER JOIN (
SELECT UP.UserID,UP.PrivilegeID,UP.PrivilegeType, RANK() OVER(PARTITION BY UserID, P.PrivilegeType, ParentID ORDER BY ISNULL(Sequence,999), P.PrivilegeID) SEQ
FROM Int_UserPrivileges UP
INNER JOIN Int_Privileges P ON P.PrivilegeID = UP.PrivilegeID AND P.PrivilegeType = UP.PrivilegeType) T
ON T.PrivilegeID = UP.PrivilegeID AND T.PrivilegeType = UP.PrivilegeType AND T.UserID = UP.UserID

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportTypes')
	EXEC sp_rename 'ImportTypes', 'Int_ExcelImportTypes';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportProcedures')
	EXEC sp_rename 'ImportProcedures', 'Int_ExcelImportProcedures';
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportDetails')
	EXEC sp_rename 'ImportDetails', 'Int_ExcelImportDetails';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaImportTypes_PK')
	EXEC sp_rename 'Int_ExcelImportTypes.aaaaaImportTypes_PK', 'PK_Int_ExcelImportTypes', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaImportDetails_PK')
	EXEC sp_rename 'Int_ExcelImportDetails.aaaaaImportDetails_PK', 'PK_Int_ExcelImportDetails', 'INDEX';
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'aaaaaImportProcedures_PK')
	EXEC sp_rename 'Int_ExcelImportProcedures.aaaaaImportProcedures_PK', 'PK_Int_ExcelImportProcedures', 'INDEX';
