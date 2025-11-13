IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ProcedureType' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'INT_FieldProcedure'))
BEGIN
	ALTER TABLE INT_FieldProcedure ADD ProcedureType INT NULL
END

EXEC('UPDATE INT_FieldProcedure SET ProcedureType = 1 WHERE ProcedureType IS NULL')
EXEC('ALTER TABLE INT_FieldProcedure ALTER COLUMN ProcedureType INT NOT NULL')

--Create table (SMSSendingLog)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'SMSSendingLog') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE SMSSendingLog(
	  [ID] INT NOT NULL IDENTITY(1,1),
      [TriggerID] INT NOT NULL ,
	  [SendingTime] DATETIME NOT NULL,
	  [ProcedureName] NVARCHAR(50) NOT NULL ,
      [URL] NVARCHAR(200) NOT NULL ,
	  [UserName] NVARCHAR(50) NOT NULL ,
	  [Password] NVARCHAR(50) NOT NULL ,
	  [Sender] NVARCHAR(50) NOT NULL ,
	  [Mobile] NVARCHAR(50) NOT NULL ,
	  [Contents] NVARCHAR(400) NOT NULL ,
	  [Request] NVARCHAR(400) NOT NULL ,
	  [Response] NVARCHAR(400) NULL
)

PRINT 'Table SMSSendingLog added successfully.'
END
ELSE
PRINT 'Table SMSSendingLog already Exists.'