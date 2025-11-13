
IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ConstantValues' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_DataTransferType'))
BEGIN
	EXEC('ALTER TABLE Int_DataTransferType ADD ConstantValues NVARCHAR(max)')
END