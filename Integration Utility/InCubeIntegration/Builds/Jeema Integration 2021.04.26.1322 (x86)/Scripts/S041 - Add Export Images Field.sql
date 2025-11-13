IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 45 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (45,2,14)
END
IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 45)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (45,3,'Export Images')
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImagesExportPath')
BEGIN
	CREATE TABLE ImagesExportPath
	(
	ID INT IDENTITY(1,1),
	TriggerID INT NOT NULL,
	RouteHistoryID INT NOT NULL,
	ReadingID INT NOT NULL,
	FieldID INT NOT NULL,
	ImageID INT NOT NULL,
	ImagePath nvarchar(max),
	ExportDate DATETIME,
	ResultID INT,
	Message NVARCHAR(max)
	CONSTRAINT [PK_Int_ImagesExportPath] PRIMARY KEY CLUSTERED (TriggerID ASC,RouteHistoryID ASC,ReadingID ASC,FieldID ASC,ImageID ASC)
	)
END

IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'ImageSendingStatus')
BEGIN
	EXEC('CREATE VIEW ImageSendingStatus AS
	SELECT E.RouteHistoryID,E.ReadingID,E.FieldID,E.ImageID,E.ResultID FROM ImagesExportPath E
	INNER JOIN (SELECT MAX(ID) ID FROM ImagesExportPath GROUP BY RouteHistoryID,ReadingID,FieldID,ImageID) R ON R.ID = E.ID')
END
