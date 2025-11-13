IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'DropCreateStagingTables' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_ExcelImportTypes'))
BEGIN
	EXEC('ALTER TABLE Int_ExcelImportTypes ADD DropCreateStagingTables INT')
END
EXEC('UPDATE Int_ExcelImportTypes SET DropCreateStagingTables = 1 WHERE DropCreateStagingTables IS NULL')
EXEC('ALTER TABLE Int_ExcelImportTypes ALTER COLUMN DropCreateStagingTables INT NOT NULL')

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ImportTypeID' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_ActionTrigger'))
BEGIN
	EXEC('ALTER TABLE Int_ActionTrigger ADD ImportTypeID INT')
END