--Create table (Int_ActionTrigger)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_ActionTrigger') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_ActionTrigger(
	  [ID] INT IDENTITY(1,1) NOT NULL ,
      [SessionID] INT NOT NULL ,
      [TaskID] INT NOT NULL ,
      [ActionID] INT NOT NULL ,
      [FieldID] INT NOT NULL ,
      [RunTimeStart] DATETIME NOT NULL ,
      [RunTimeEnd] DATETIME NULL ,
	  [TotalRows] INT NULL
 );

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_ServiceTriggers')
BEGIN

EXEC('SET IDENTITY_INSERT Int_ActionTrigger ON
INSERT INTO Int_ActionTrigger (ID,SessionID,TaskID,ActionID,FieldID,RunTimeStart,RunTimeEnd)
SELECT ID,SessionID,ST.TaskID,ST.ActionID,ISNULL(TA.FieldID,-1),RunTimeStart,RunTimeEnd 
FROM Int_ServiceTriggers ST
LEFT JOIN Int_TaskAction TA ON TA.ActionID = ST.ActionID
ORDER BY ST.ID
SET IDENTITY_INSERT Int_ActionTrigger OFF

DROP TABLE Int_ServiceTriggers')

END

PRINT 'Table Int_ActionTrigger added successfully.'
END
ELSE
PRINT 'Table Int_ActionTrigger already Exists.'


--Create table (ERROR_TRACK)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'ERROR_TRACK') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE ERROR_TRACK(
      [ErrorNumber] NVARCHAR(200) NULL ,
      [ProcedureName] NVARCHAR(200) NULL ,
      [ErrorLine] NVARCHAR(200) NULL ,
      [ErrorMessage] NVARCHAR(200) NULL ,
      [ErrorDate] NVARCHAR(200) NULL 
 );

PRINT 'Table ERROR_TRACK added successfully.'
END
ELSE
PRINT 'Table ERROR_TRACK already Exists.'