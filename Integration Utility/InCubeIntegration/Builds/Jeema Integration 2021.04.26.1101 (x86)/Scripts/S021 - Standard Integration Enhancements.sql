IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ParameterType' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'INT_FieldProcParams'))
BEGIN
	ALTER TABLE INT_FieldProcParams ADD ParameterType INT NULL
END

UPDATE INT_FieldProcParams SET ParameterValue = ParameterName WHERE ParameterValue IS NULL

EXEC('
UPDATE INT_FieldProcParams SET ParameterType = 1 WHERE ParameterName IN (''@TriggerID'',''@UserID'',''@EmployeeID'',''@WarehouseID'')
UPDATE INT_FieldProcParams SET ParameterType = 3 WHERE ParameterName IN (''@FromDate'',''@ToDate'',''@StockDate'')
UPDATE INT_FieldProcParams SET ParameterType = 2 WHERE ParameterType IS NULL
')

ALTER TABLE INT_FieldProcParams ALTER COLUMN ParameterValue nvarchar(200) NOT NULL
EXEC('ALTER TABLE INT_FieldProcParams ALTER COLUMN ParameterType INT NOT NULL')