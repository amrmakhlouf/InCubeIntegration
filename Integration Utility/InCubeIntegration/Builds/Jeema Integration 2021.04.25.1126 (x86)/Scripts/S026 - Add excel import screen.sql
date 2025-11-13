IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 11 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (11,1,'Excel Import',0)
END

--Create table (ImportTypes)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'ImportTypes') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE ImportTypes(
      [ImportTypeID] INT NOT NULL ,
      [Name] NVARCHAR(50) NOT NULL ,
      [Description] NVARCHAR(400) NULL ,
      [TableName] NVARCHAR(50) NOT NULL ,

 CONSTRAINT [aaaaaImportTypes_PK] PRIMARY KEY CLUSTERED 
(
  ImportTypeID
));

PRINT 'Table ImportTypes added successfully.'
END
ELSE
PRINT 'Table ImportTypes already Exists.'

--Create table (ImportDetails)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'ImportDetails') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE ImportDetails(
      [ImportTypeID] INT NOT NULL ,
	  [Sequence] INT NOT NULL ,
      [FieldName] NVARCHAR(50) NOT NULL ,
      [FieldType] INT NOT NULL ,

 CONSTRAINT [aaaaaImportDetails_PK] PRIMARY KEY CLUSTERED 
(
  ImportTypeID,FieldName
));

PRINT 'Table ImportDetails added successfully.'
END
ELSE
PRINT 'Table ImportDetails already Exists.'

--Create table (ImportProcedures)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'ImportProcedures') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE ImportProcedures(
      [ImportTypeID] INT NOT NULL ,
      [Sequence] INT NOT NULL ,
      [ProcedureName] NVARCHAR(50) NOT NULL ,

 CONSTRAINT [aaaaaImportProcedures_PK] PRIMARY KEY CLUSTERED 
(
  ImportTypeID,Sequence
));

PRINT 'Table ImportProcedures added successfully.'
END
ELSE
PRINT 'Table ImportProcedures already Exists.'