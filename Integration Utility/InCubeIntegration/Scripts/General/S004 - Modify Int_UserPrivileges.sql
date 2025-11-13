IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'Sequence' AND object_id = OBJECT_ID('Int_UserPrivileges'))
BEGIN
ALTER TABLE Int_UserPrivileges ADD Sequence INT NULL
END
IF EXISTS (SELECT * FROM sys.columns WHERE name = 'Value' AND object_id = OBJECT_ID('Int_UserPrivileges'))
BEGIN
ALTER TABLE Int_UserPrivileges DROP COLUMN Value
END