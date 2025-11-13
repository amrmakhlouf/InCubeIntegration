IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ConnectionID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FieldProcedure'))
BEGIN
	EXEC('ALTER TABLE Int_FieldProcedure ADD ConnectionID INT')
END

--Add missing lookups
IF NOT EXISTS (SELECT * FROM Int_Lookups WHERE LookupName = 'ProcType' AND ID = 6)
	INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES ('ProcType',6,'OracleProcedure');