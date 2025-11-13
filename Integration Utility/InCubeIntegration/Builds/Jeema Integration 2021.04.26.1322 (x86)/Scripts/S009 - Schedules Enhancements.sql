IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'CreationDate' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Tasks'))
BEGIN
	ALTER TABLE Int_Tasks ADD CreationDate DATETIME NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UpdatedBy' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Tasks'))
BEGIN
	ALTER TABLE Int_Tasks ADD UpdatedBy INT NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'UpdatingDate' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Tasks'))
BEGIN
	ALTER TABLE Int_Tasks ADD UpdatingDate DATETIME NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'OrganizationID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Tasks'))
BEGIN
	ALTER TABLE Int_Tasks ADD OrganizationID INT NULL
	
	EXEC('UPDATE T SET T.OrganizationID = O.OrganizationID
	FROM Int_Tasks T
	INNER JOIN (SELECT DISTINCT A.TaskID,A.OrganizationID FROM Int_TaskAction A) O ON T.TaskID = O.TaskID')

	EXEC ('ALTER TABLE Int_Tasks ALTER COLUMN OrganizationID INT NOT NULL')

	ALTER TABLE Int_TaskAction DROP COLUMN OrganizationID
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Day' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_TaskSchedule'))
BEGIN
	ALTER TABLE Int_TaskSchedule ADD Day INT NULL
END

--Create table (Int_ExecutionDetails)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_ExecutionDetails') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN

CREATE TABLE Int_ExecutionDetails(
      [ID] INT IDENTITY(1,1) NOT NULL ,
      [TriggerID] INT NOT NULL ,
      [OrganizationID] INT NOT NULL,
      [Filter1ID] INT NULL ,
      [Filter1Value] NVARCHAR(200) NULL ,
      [Filter2ID] INT NULL ,
      [Filter2Value] NVARCHAR(200) NULL ,
      [Filter3ID] INT NULL ,
      [Filter3Value] NVARCHAR(200) NULL ,
      [RunTimeStart] DATETIME NOT NULL ,
      [RunTimeEnd] DATETIME NULL ,
	  [ResultID] INT NULL ,
	  [Inserted] INT NULL ,
	  [Updated] INT NULL ,
	  [Skipped] INT NULL ,
	  [FilePath] NVARCHAR(300) NULL ,
	  [Message] nvarchar(max) NULL
 );

PRINT 'Table Int_ExecutionDetails added successfully.'
END
ELSE
PRINT 'Table Int_ExecutionDetails already Exists.'