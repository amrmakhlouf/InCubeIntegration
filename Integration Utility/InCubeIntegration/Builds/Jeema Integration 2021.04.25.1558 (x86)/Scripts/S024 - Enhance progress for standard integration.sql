IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ExecutionTableName' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'INT_FieldProcedure'))
BEGIN
	ALTER TABLE INT_FieldProcedure ADD ExecutionTableName NVARCHAR(50) NULL
END