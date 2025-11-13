IF EXISTS (SELECT * FROM sys.objects WHERE name = 'aaaaaFilesRemovalJob_PK')
BEGIN
	ALTER TABLE FilesRemovalJob DROP CONSTRAINT aaaaaFilesRemovalJob_PK
END

ALTER TABLE FilesRemovalJob ALTER COLUMN FileExtension NVARCHAR(50) NOT NULL
EXEC('ALTER TABLE FilesRemovalJob ADD CONSTRAINT [aaaaaFilesRemovalJob_PK] PRIMARY KEY CLUSTERED (FolderPath,FileExtension);')