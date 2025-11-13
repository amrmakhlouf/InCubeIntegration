IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'OrganizationID' AND object_id = OBJECT_ID('Int_Configuration'))
BEGIN
ALTER TABLE Int_Configuration ADD OrganizationID INT NOT NULL DEFAULT (-1)
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'DataType' AND object_id = OBJECT_ID('Int_Configuration'))
BEGIN
ALTER TABLE Int_Configuration ADD DataType INT NOT NULL DEFAULT (1)
END

ALTER TABLE Int_Configuration ALTER COLUMN KeyValue NVARCHAR(400) NOT NULL
ALTER TABLE Int_Configuration DROP CONSTRAINT aaaaaInt_Configuration_PK
ALTER TABLE Int_Configuration ADD CONSTRAINT [aaaaaInt_Configuration_PK] PRIMARY KEY CLUSTERED (KeyName,OrganizationID)
