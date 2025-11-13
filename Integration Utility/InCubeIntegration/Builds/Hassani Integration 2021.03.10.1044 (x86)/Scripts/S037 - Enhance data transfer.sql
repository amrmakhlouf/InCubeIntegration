IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'HasIdentityColumn' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_DataTransferType'))
BEGIN
	EXEC('ALTER TABLE Int_DataTransferType ADD HasIdentityColumn BIT')
	EXEC('UPDATE Int_DataTransferType SET HasIdentityColumn = 0')
	EXEC('ALTER TABLE Int_DataTransferType ALTER COLUMN HasIdentityColumn BIT NOT NULL')
END

