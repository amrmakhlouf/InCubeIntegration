--Create table (Int_FieldProcedure)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_FieldProcedure') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_FieldProcedure(
      [FieldID] INT NOT NULL ,
	  [Sequence] INT NOT NULL ,
      [ProcedureName] NVARCHAR(200) NOT NULL ,

 CONSTRAINT [aaaaaInt_FieldProcedure_PK] PRIMARY KEY CLUSTERED 
(
  FieldID,Sequence
));

PRINT 'Table Int_FieldProcedure added successfully.'
END
ELSE
PRINT 'Table Int_FieldProcedure already Exists.'

--Create table (Int_FieldProcParams)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'Int_FieldProcParams') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE Int_FieldProcParams(
      [FieldID] INT NOT NULL ,
      [Sequence] INT NOT NULL ,
      [ParameterName] NVARCHAR(200) NOT NULL ,
	  [ParameterValue] NVARCHAR(200) NULL ,

 CONSTRAINT [aaaaaInt_FieldProcParams_PK] PRIMARY KEY CLUSTERED 
(
  FieldID,Sequence,ParameterName
));

PRINT 'Table Int_FieldProcParams added successfully.'
END
ELSE
PRINT 'Table Int_FieldProcParams already Exists.'