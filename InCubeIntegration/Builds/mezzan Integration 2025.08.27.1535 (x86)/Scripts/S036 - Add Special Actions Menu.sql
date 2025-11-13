UPDATE Int_Privileges SET ParentID = 14 WHERE PrivilegeID = 43
UPDATE Int_Field SET ActionType = 3 WHERE FieldID = 43
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 44 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (44,2,14)
END
IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 44)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (44,3,'Database Backup')
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 45)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (45,1,'Serials Stock')
END
IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 47)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (47,1,'Areas')
END
IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 47)
BEGIN
INSERT INTO Int_Privileges(PrivilegeID,PrivilegeType,Description,ParentID) VALUES (47,2,'',7)
END
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_DatabaseBackupJobs')
BEGIN
	CREATE TABLE Int_DataBaseBackupJobs
	(
		JobID INT NOT NULL,
		JobName nvarchar(200) NOT NULL,
		DatabaseName nvarchar(50) NOT NULL,
		BackupPath nvarchar(max) NOT NULL,
		IsDeleted BIT NOT NULL,
		RefJobID INT NULL,
		CONSTRAINT [PK_Int_DataBaseBackup] PRIMARY KEY CLUSTERED (JobID ASC)
	)
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Int_FilesManagementJobs')
BEGIN
	CREATE TABLE Int_FilesManagementJobs
	(
		JobID INT NOT NULL,
		JobName nvarchar(200) NOT NULL,
		JobType INT NOT NULL,
		SourceFolder nvarchar(max) NOT NULL,
		FileExtension nvarchar(10) NULL,
		ModifyAge INT NULL,
		AgeTimeUnit INT NULL,
		DestinationFolder nvarchar(max) NULL,
		IsDeleted INT NOT NULL,
		RefJobID INT NULL,
		CONSTRAINT [PK_Int_FilesManagementJobs] PRIMARY KEY CLUSTERED (JobID ASC)
	)
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FilesRemovalJob')
BEGIN
	EXEC('
	INSERT INTO Int_FilesManagementJobs (JobID,JobName,JobType,SourceFolder,FileExtension,ModifyAge,AgeTimeUnit,IsDeleted)
	SELECT RANK() OVER(ORDER BY FolderPath,FileExtension,DaysOld)
	, ''Delete ('' + FileExtension + '') files aged more than '' + CAST(Daysold AS nvarchar(5)) + '' days from ('' + FolderPath + '')'' 
	, 1, FolderPath, FileExtension, DaysOld, 4, 0
	FROM FilesRemovalJob
	')
	DROP TABLE FilesRemovalJob;
END

UPDATE Int_Field SET FieldName = 'Files Jobs' WHERE FieldID = 33;
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 33 AND FilterID = 11)
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (33,11)
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 44 AND FilterID = 12)
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (44,12)

UPDATE Int_Privileges SET Description = 'Import Master Data' WHERE PrivilegeID = 7 AND PrivilegeType = 1
UPDATE Int_Privileges SET Description = 'Send Transactions' WHERE PrivilegeID = 8 AND PrivilegeType = 1
UPDATE Int_Privileges SET Description = 'Special Functions' WHERE PrivilegeID = 14 AND PrivilegeType = 1
