IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ReadExecutionDetails' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FieldProcedure'))
BEGIN
	ALTER TABLE Int_FieldProcedure ADD ReadExecutionDetails BIT NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ExecDetailsReadQry' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FieldProcedure'))
BEGIN
	ALTER TABLE Int_FieldProcedure ADD ExecDetailsReadQry NVARCHAR(max) NULL
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'DefaultCheck' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_UserPrivileges'))
BEGIN
	ALTER TABLE Int_UserPrivileges ADD DefaultCheck BIT NULL
END