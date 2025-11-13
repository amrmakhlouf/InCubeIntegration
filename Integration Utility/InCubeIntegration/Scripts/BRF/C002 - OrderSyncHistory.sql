--Create table (OrderSyncHistory)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'OrderSyncHistory') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE OrderSyncHistory(
      [ID] INT IDENTITY(1,1) NOT NULL ,
	  [SendingProcessID] INT NOT NULL,
      [OrderID] NVARCHAR(200) NOT NULL ,
      [CustomerID] INT NOT NULL ,
      [AttemptDate] DATETIME NOT NULL ,
      [Result] NVARCHAR(200) NOT NULL 
 );

PRINT 'Table OrderSyncHistory added successfully.'
END
ELSE
PRINT 'Table OrderSyncHistory already Exists.'