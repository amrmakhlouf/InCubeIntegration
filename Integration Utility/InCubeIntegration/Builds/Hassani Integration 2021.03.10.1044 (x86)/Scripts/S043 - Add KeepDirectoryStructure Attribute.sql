IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'KeepDirectoryStructure' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_FilesManagementJobs'))
BEGIN
	EXEC('ALTER TABLE Int_FilesManagementJobs ADD KeepDirectoryStructure BIT')
	EXEC('UPDATE Int_FilesManagementJobs SET KeepDirectoryStructure = 0')
	EXEC('ALTER TABLE Int_FilesManagementJobs ALTER COLUMN KeepDirectoryStructure BIT NOT NULL')
END

