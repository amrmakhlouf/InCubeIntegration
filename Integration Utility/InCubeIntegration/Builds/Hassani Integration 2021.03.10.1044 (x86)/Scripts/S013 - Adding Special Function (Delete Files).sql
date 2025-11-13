IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 33 AND ActionType = 3)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (33,3,'Delete Files');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 33 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (33,1,7);
END

--Create table (FilesRemovalJob)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'FilesRemovalJob') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE FilesRemovalJob(
      [FolderPath] NVARCHAR(400) NOT NULL ,
      [FileExtension] NVARCHAR(200) NOT NULL ,
      [DaysOld] INT NOT NULL ,

 CONSTRAINT [aaaaaFilesRemovalJob_PK] PRIMARY KEY CLUSTERED 
(
  FolderPath,FileExtension
));

PRINT 'Table FilesRemovalJob added successfully.'
END
ELSE
PRINT 'Table FilesRemovalJob already Exists.'