IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'Int_ExcelImportSheets')
BEGIN
	CREATE TABLE Int_ExcelImportSheets
	(
	ImportTypeID INT NOT NULL,
	SheetNo INT NOT NULL,
	SheetDescription NVARCHAR(50) NOT NULL,
	StagingTable NVARCHAR(50) NOT NULL,
	 CONSTRAINT [PK_Int_ExcelImportSheets] PRIMARY KEY CLUSTERED 
	(
		[ImportTypeID] ASC,
		[SheetNo] ASC
	)) ON [PRIMARY];

	EXEC('INSERT INTO Int_ExcelImportSheets
	SELECT ImportTypeID,1,Name,TableName FROM Int_ExcelImportTypes')

END

IF EXISTS (SELECT * FROM sys.columns WHERE name = 'TableName' AND object_id = OBJECT_ID('Int_ExcelImportTypes'))
BEGIN
	ALTER TABLE Int_ExcelImportTypes DROP COLUMN TableName
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'Int_ExcelImportColumns')
BEGIN
	CREATE TABLE [dbo].[Int_ExcelImportColumns](
		[ImportTypeID] [int] NOT NULL,
		[SheetNo] [int] NOT NULL,
		[Sequence] [int] NOT NULL,
		[FieldName] [nvarchar](50) NOT NULL,
		[FieldType] [int] NOT NULL,
	 CONSTRAINT [PK_Int_ExcelImportColumns] PRIMARY KEY CLUSTERED 
	(
		[ImportTypeID] ASC,
		[SheetNo] ASC,
		[FieldName] ASC
	)) ON [PRIMARY]

	INSERT INTO Int_ExcelImportColumns
	SELECT ImportTypeID,1,Sequence,FieldName,FieldType FROM Int_ExcelImportDetails;

	DROP TABLE Int_ExcelImportDetails;
END