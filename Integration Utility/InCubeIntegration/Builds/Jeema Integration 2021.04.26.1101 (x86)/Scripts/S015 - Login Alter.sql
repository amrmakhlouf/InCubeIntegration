IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'VersionNo' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Session'))
BEGIN
	ALTER TABLE Int_Session ADD VersionNo NVARCHAR(20) NULL
END