IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ComparisonOperator' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FilesManagementJobs'))
BEGIN
	EXEC('ALTER TABLE Int_FilesManagementJobs ADD ComparisonOperator INT')
	EXEC('UPDATE Int_FilesManagementJobs SET ComparisonOperator = 2')
	EXEC('ALTER TABLE Int_FilesManagementJobs ALTER COLUMN ComparisonOperator INT NOT NULL')
END

EXEC('
DELETE FROM Int_Lookups WHERE LookupName = ''ComparisonOperator'';

--ActionType
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ComparisonOperator'',1,''EqualTo'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ComparisonOperator'',2,''GreaterThan'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ComparisonOperator'',3,''GreaterThanOrEqualTo'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ComparisonOperator'',4,''LessThan'');
INSERT INTO Int_Lookups (LookupName,ID,Value) VALUES (''ComparisonOperator'',5,''LessThanOrEqualTo'');
')
