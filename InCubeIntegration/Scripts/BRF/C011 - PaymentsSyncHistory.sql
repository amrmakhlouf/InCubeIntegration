--Create table (PaymentsSyncHistory)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'PaymentsSyncHistory') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE PaymentsSyncHistory(
      [ID] INT IDENTITY(1,1) NOT NULL ,
	  [SendingProcessID] INT NOT NULL,
      [CustomerPaymentID] NVARCHAR(200) NOT NULL ,
      [TransactionID] NVARCHAR(200) NULL ,
      [CustomerID] INT NOT NULL ,
      [AttemptDate] DATETIME NOT NULL ,
      [Result] NVARCHAR(200) NOT NULL 
 );

PRINT 'Table PaymentsSyncHistory added successfully.'
END
ELSE
PRINT 'Table PaymentsSyncHistory already Exists.'

EXEC('
CREATE VIEW PaymentSendingStatus AS
SELECT CustomerPaymentID,TransactionID,CustomerID,Result,AttemptDate FROM PaymentsSyncHistory WHERE ID IN (
SELECT MAX(ID) FROM PaymentsSyncHistory GROUP BY CustomerPaymentID,TransactionID)')

INSERT INTO PaymentsSyncHistory (SendingProcessID,CustomerPaymentID,TransactionID,CustomerID,AttemptDate,Result)
SELECT 0,CustomerPaymentID,TransactionID,CustomerID,GETDATE(),'Success' from CustomerPayment WHERE Synchronized = 1

INSERT INTO PaymentsSyncHistory (SendingProcessID,CustomerPaymentID,TransactionID,CustomerID,AttemptDate,Result)
SELECT 0,CustomerPaymentID,'',CustomerID,GETDATE(),'Success' from CustomerUnallocatedPayment WHERE Synchronised = 1