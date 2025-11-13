IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 12 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (12,1,'Transactions Management',0)
END

--Create table (TransactionsManagementLog)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'TransactionsManagementLog') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE TransactionsManagementLog(
      [TransactionID] NVARCHAR(50) NOT NULL ,
	  [TransactionTypeID] NVARCHAR(50) NOT NULL ,
      [CustomerID] INT NOT NULL ,
      [OutletID] INT NOT NULL ,
      [Action] NVARCHAR(20) NOT NULL ,
	  [ActionDate] DATETIME NOT NULL ,
	  [ToCustomerID] INT NULL ,
	  [ToOutletID] INT NULL
	  )

PRINT 'Table TransactionsManagementLog added successfully.'
END
ELSE
PRINT 'Table TransactionsManagementLog already Exists.'

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'sp_GetListOfAllCustomers')
BEGIN
EXEC('
CREATE PROCEDURE sp_GetListOfAllCustomers AS
BEGIN

SELECT CO.CustomerID,CO.OutletID
,CAST(CO.CustomerID AS NVARCHAR(10)) + '':'' + CAST(CO.OutletID AS NVARCHAR(10)) CustOutID
, CO.CustomerCode,COL.Description CustomerName
FROM CustomerOutlet CO
LEFT JOIN CustomerOutletLanguage COL ON COL.CustomerID = CO.CustomerID AND COL.OutletID = CO.OutletID AND COL.LanguageID = 1
WHERE CO.InActive = 0

END
')
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'sp_GetListOfTransactionTypes')
BEGIN
EXEC('
CREATE PROCEDURE sp_GetListOfTransactionTypes AS
BEGIN

SELECT TypeID,TypeDesc From TransactionsTypes ORDER BY ID

END
')
END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'sp_GetCustomerTransactions')
BEGIN
EXEC('
CREATE PROCEDURE sp_GetCustomerTransactions
(
@CustomerID NVARCHAR(20),
@OutletID NVARCHAR(20),
@TransactionTypeID NVARCHAR(20),
@FromDate DATE,
@ToDate DATE
)
AS
BEGIN

DECLARE @Query NVARCHAR(max) = ''
SELECT TransactionID,[Type] TransactionTypeID, T.TypeDesc TransactionType, TransactionDate, ABS(BalanceChange) Amount
FROM BalanceChangeDetails B
INNER JOIN TransactionsTypes T ON T.TypeID = B.[Type]
WHERE CustomerID = '' + @CustomerID + '' AND OutletID = '' + @OutletID + ''
AND TransactionDate >= '''''' + CAST(@FromDate AS nvarchar(20)) + ''''''
AND TransactionDate < DATEADD(DD,1,'''''' + CAST(@ToDate AS nvarchar(20)) + '''''')''

IF (@TransactionTypeID <> '''')
	SET @Query = @Query + '' AND [Type] = '''''' + @TransactionTypeID + '''''''';

EXEC(@Query);

END
')

END

IF NOT EXISTS (SELECT * FROM sys.procedures WHERE NAME = 'sp_ApplyActionOnTransaction')
BEGIN
EXEC('
CREATE PROCEDURE sp_ApplyActionOnTransaction
(
@TransactionID NVARCHAR(20),
@TransactionTypeID NVARCHAR(20),
@CustomerID NVARCHAR(20),
@OutletID NVARCHAR(20),
@Action NVARCHAR(20),
@Result NVARCHAR(max),
@ToCustomerID NVARCHAR(20),
@ToOutletID NVARCHAR(20)
)
AS
BEGIN

INSERT INTO TransactionsManagementLog (TransactionID,TransactionTypeID,CustomerID,OutletID,Action,ActionDate,ToCustomerID,ToOutletID)
VALUES (@TransactionID,@TransactionTypeID,@CustomerID,@OutletID,@Action,GETDATE(),@ToCustomerID,@ToOutletID);

END
')
END