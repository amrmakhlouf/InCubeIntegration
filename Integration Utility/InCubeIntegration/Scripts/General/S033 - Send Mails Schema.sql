IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 13 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (13,1,'Mail Configuration',0)
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_MailSenderProfile')
	EXEC('DROP TABLE Int_MailSenderProfile');
EXEC('
CREATE TABLE Int_MailSenderProfile
(
SenderProfileID INT IDENTITY(1,1),
ProfileName NVARCHAR(100),
Host NVARCHAR(100),
Port INT,
MailAddress NVARCHAR(100),
DisplayName NVARCHAR(100),
Password NVARCHAR(100),
EnableSSL BIT,
IsDeleted BIT,
RefProfileID BIT
)
')

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_MailTemplate')
	EXEC('DROP TABLE Int_MailTemplate');
EXEC('
CREATE TABLE Int_MailTemplate
(
MailTemplateID INT,
TemplateName NVARCHAR(100),
SenderProfileID INT,
Subject NVARCHAR(200),
Header NVARCHAR(max),
Footer NVARCHAR(max),
IsDeleted BIT,
RefMailTemplateID INT
)
')

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_MailTemplateRecipients')
	EXEC('DROP TABLE Int_MailTemplateRecipients');
EXEC('
CREATE TABLE Int_MailTemplateRecipients
(
MailTemplateID INT,
RecipientAddress NVARCHAR(50),
RecipientType INT
)
')

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_PreparedMails')
	EXEC('DROP TABLE Int_PreparedMails');
EXEC('
CREATE TABLE Int_PreparedMails
(
TriggerID INT,
MailNo INT,
SendingTime DATETIME,
ResultID INT,
ErrorMessage NVARCHAR(MAX)
)
')

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_MailBody')
	EXEC('DROP TABLE Int_MailBody');
EXEC('
CREATE TABLE Int_MailBody
(
TriggerID INT,
MailNo INT,
[LineNo] INT,
LineText NVARCHAR(MAX)
)
')

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'MailTemplateID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FieldProcedure'))
BEGIN
	EXEC('ALTER TABLE Int_FieldProcedure ADD MailTemplateID INT NULL')
END
