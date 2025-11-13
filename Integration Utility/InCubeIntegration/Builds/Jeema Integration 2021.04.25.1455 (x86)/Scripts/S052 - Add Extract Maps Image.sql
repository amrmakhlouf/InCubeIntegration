IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 49)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (49,3,'Extract Transactions Map Images')
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 49 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (49,2,14)
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_TransactionImage')
BEGIN
	CREATE TABLE [dbo].[tbl_TransactionImage](
		[TransactionID] [nvarchar](200) NOT NULL,
		[CustomerID] [int] NOT NULL,
		[OutletID] [int] NOT NULL,
		[Image] [image] NOT NULL,
	 CONSTRAINT [PK_tbl_TransactionImage] PRIMARY KEY CLUSTERED 
	(
		[TransactionID] ASC,
		[CustomerID] ASC,
		[OutletID] ASC
	) ON [PRIMARY]);
END

