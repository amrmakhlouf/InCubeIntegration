IF OBJECT_ID('dbo.sp_UpdateOrders') IS NULL
	EXEC('CREATE PROCEDURE dbo.sp_UpdateOrders AS SET NOCOUNT ON;')
GO
ALTER PROCEDURE [dbo].[sp_UpdateOrders]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN

DECLARE
@CustomerID INT,
@OutletID INT,
@AccountID INT,
@OrderID NVARCHAR(20),
@OrderDate DATETIME,
@DeliveryDate DATETIME,
@LPO NVARCHAR(100),
@EmployeeID INT,
@Status INT,
@ShipToCode NVARCHAR(50),
@ItemCode NVARCHAR(50),
@UOM NVARCHAR(20),
@Quantity DECIMAL(19,9),
@IsFOC BIT,
@ItemID INT,
@PackTypeID INT,
@PackID INT,
@NetTotal DECIMAL(19,9),
@GrossTotal DECIMAL(19,9),
@Discount DECIMAL(19,9),
@Price DECIMAL(19,9),
@LineNumber INT,
@EmployeeCode NVARCHAR(20),
@Ignored BIT,
@IsNew BIT,
@ResultID INT,
@ErrorCatched BIT,
@LOOP INT,
@Message NVARCHAR(max),
@ProcessID INT;

DECLARE LOOP1 CURSOR STATIC FOR
SELECT O.TransactionNumber COLLATE Arabic_CI_AS,O.TransactionDate, O.DeliveryDate
, O.LPO_Number COLLATE Arabic_CI_AS
, CASE O.OrderStatus WHEN 1 THEN 3 ELSE 13 END, ShipToCode COLLATE Arabic_CI_AS
, ISNULL(O.EmployeeCode,'') COLLATE Arabic_CI_AS
FROM MDStagingTables..SalesOrderHeader O
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = RIGHT(O.ShipToCode COLLATE Arabic_CI_AS,LEN(O.ShipToCode)-1)
LEFT JOIN Employee E ON E.EmployeeCode = O.EmployeeCode COLLATE Arabic_CI_AS
WHERE O.IsProcessed = 0;

OPEN LOOP1

FETCH NEXT FROM LOOP1
INTO @OrderID,@OrderDate,@DeliveryDate,@LPO,@Status,@ShipToCode,@EmployeeCode;

UPDATE Int_ActionTrigger SET TotalRows = @@CURSOR_ROWS WHERE ID = @TriggerID;

WHILE(@@FETCH_STATUS=0)
BEGIN

BEGIN TRY 

SET @LOOP = 1;
SET @ResultID = 2;
SET @Ignored = 1;
SET @IsNew = 0;
SET @Message = '';
SET @ErrorCatched = 0;
SET @GrossTotal = 0;
SET @NetTotal = 0;
SET @Discount = 0;

INSERT INTO Int_ExecutionDetails (TriggerID,OrganizationID,RunTimeStart,Filter1ID,Filter1Value)
VALUES (@TriggerID,1,GETDATE(),8,@OrderID);
SET @ProcessID = SCOPE_IDENTITY();

BEGIN TRANSACTION T1

IF NOT EXISTS (SELECT EmployeeID FROM Employee WHERE EmployeeCode = @EmployeeCode AND EmployeeTypeID = 2)
BEGIN
	SET @Message = 'Salesman [' + @EmployeeCode + '] is not defined in InVan';
	GOTO DECIDE;
END
SET @EmployeeID = (SELECT EmployeeID FROM Employee WHERE EmployeeCode = @EmployeeCode);

IF NOT EXISTS (SELECT* FROM CustomerOutlet WHERE CustomerCode = @ShipToCode)
BEGIN
	SET @Message = 'ShipTo Code [' + @ShipToCode + '] is not defined in InVan';
	GOTO DECIDE;
END
SELECT @CustomerID = CustomerID, @OutletID = OutletID FROM CustomerOutlet WHERE CustomerCode = @ShipToCode;

IF NOT EXISTS (SELECT * FROM SalesOrder WHERE OrderID = @OrderID AND CustomerID = @CustomerID AND OutletID = @OutletID)
BEGIN
SET @IsNew = 1;
INSERT INTO SalesOrder (CustomerID,OutletID,OrderID,DesiredDeliveryDate,OrderDate,Synchronized,EmployeeID,OrderStatusID
,Downloaded,LPO,NetTotal,PromotedDiscount,Discount,GrossTotal,Tax,CreatedBy,CreatedDate,CreationSource,DivisionID
,OrganizationID,OrderTypeID)
VALUES (@CustomerID,@OutletID,@OrderID,@DeliveryDate,@OrderDate,1,@EmployeeID,@Status
,1,@LPO,0,0,0,0,0,@UserID,GETDATE(),10,-1
,1,1);
END

ELSE BEGIN
UPDATE SalesOrder SET EmployeeID = @EmployeeID, DesiredDeliveryDate = @DeliveryDate, OrderStatusID = @Status, LPO = @LPO
WHERE OrderID = @OrderID AND CustomerID = @CustomerID AND OutletID = @OutletID;
END

IF (@Status = 3 AND NOT EXISTS (SELECT * FROM DeliveryAssignment WHERE OrderID = @OrderID AND CustomerID = @CustomerID AND OutletID = @OutletID))
BEGIN
INSERT INTO DeliveryAssignment (EmployeeID,OrderID,AssignmentDate,ScheduleDate,DispatcherID,DeliveryStatusID,CustomerID,OutletID,DeliveryAssignmentID,OrganizationID,DivisionID)
VALUES (@EmployeeID,@OrderID,GETDATE(),@DeliveryDate,@UserID,1,@CustomerID,@OutletID,(SELECT ISNULL(MAX(DeliveryAssignmentID),0)+1 FROM DeliveryAssignment),1,-1);
END

DECLARE LOOP2 CURSOR STATIC FOR
SELECT ItemCode,UOM,Quantity,FOC_Indicator,LineNumber
FROM MDStagingTables..SalesOrderDetail D
WHERE TransactionNumber = @OrderID

OPEN LOOP2

FETCH NEXT FROM LOOP2
INTO @ItemCode,@UOM,@Quantity,@IsFOC,@LineNumber;

WHILE(@@FETCH_STATUS=0)
BEGIN

SET @LOOP = 2;

IF NOT EXISTS (SELECT ItemID FROM Item WHERE ItemCode = @ItemCode)
BEGIN
	SET @Message = 'Item Code [' + @ItemCode + '] is not defined in InVan';
	GOTO DECIDE;
END
SET @ItemID = (SELECT ItemID FROM Item WHERE ItemCode = @ItemCode);

IF NOT EXISTS (SELECT PackTypeID FROM PackTypeLanguage WHERE Description = @UOM AND LanguageID = 1)
BEGIN
	SET @Message = 'UOM [' + @UOM + '] is not defined in InVan';
	GOTO DECIDE;
END
SET @PackTypeID = (SELECT PackTypeID FROM PackTypeLanguage WHERE Description = @UOM AND LanguageID = 1);

IF NOT EXISTS (SELECT PackID FROM Pack WHERE ItemID = @ItemID AND PackTypeID = @PackTypeID)
BEGIN
	SET @Message = 'Pack [' + @ItemCode + ',' + @UOM + '] is not defined in InVan';
	GOTO DECIDE;
END
SET @PackID = (SELECT PackID FROM Pack WHERE ItemID = @ItemID AND PackTypeID = @PackTypeID);

SET @Price = 0;
SELECT @Price = Price
FROM PriceDefinition PD
INNER JOIN PriceList PL ON PL.PriceListID = PD.PriceListID
INNER JOIN 
(SELECT PriceListID,2 Priority FROM GroupPrice WHERE GroupID IN 
(SELECT GroupID FROM CustomerOutletGroup WHERE CustomerID = @CustomerID AND OutletID = @OutletID)
UNION
SELECT CAST(KeyValue AS INT),3 FROM Configuration WHERE KeyName = 'DefaultPriceListID'
UNION
SELECT PriceListID,1 FROM CustomerPrice WHERE CustomerID = @CustomerID AND OutletID = @OutletID) CPL
ON CPL.PriceListID = PD.PriceListID
WHERE PD.PacKID = @PackID AND PL.IsDeleted = 0
ORDER BY CPL.Priority

INSERT INTO SalesOrderDetail (OrderID,PackID,Quantity,Price,Tax,Discount,CustomerID,OutletID,SalesTransactionTypeID,BasePrice,DivisionID
,FOC,DiscountTypeID,PromotedDiscount,FinalDiscount,AdditionalDiscount,ReturnReason,PackStatusID,ExpiryDate,Sequence,StockStatusID
,ItemDeliveryStatusID,SalesOrderTypeID)
VALUES (@OrderID,@PackID,@Quantity,CASE @IsFOC WHEN 0 THEN @Price ELSE 0 END,0,0,@CustomerID,@OutletID,1,@Price,-1
,0,1,0,0,0,-1,-1,'1990-01-01',@LineNumber,0,-1,-1);

SET @GrossTotal = @GrossTotal + (CASE @IsFOC WHEN 0 THEN @Price ELSE 0 END) * @Quantity;
SET @NetTotal = @NetTotal + (CASE @IsFOC WHEN 0 THEN @Price ELSE 0 END) * @Quantity;

FETCH NEXT FROM LOOP2
INTO @ItemCode,@UOM,@Quantity,@IsFOC,@LineNumber;

END

CLOSE LOOP2
DEALLOCATE LOOP2

UPDATE SalesOrder SET NetTotal = @NetTotal, GrossTotal = @GrossTotal, Discount = @Discount WHERE OrderID = @OrderID AND CustomerID = @CustomerID AND OutletID = @OutletID;
UPDATE MDStagingTables..SalesOrderHeader SET IsProcessed = 1 WHERE TransactionNumber = @OrderID AND ShipToCode = @ShipToCode;
SET @Ignored = 0;
SET @Message = 'Success';
SET @ResultID = 1;

END TRY

BEGIN CATCH
SET @ErrorCatched = 1;
ROLLBACK TRANSACTION T1;
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), 'sp_UpdateOrders',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
SET @Message = ERROR_MESSAGE();
END CATCH

DECIDE:
IF (@Ignored = 0)
BEGIN
	COMMIT TRANSACTION T1
END
ELSE BEGIN
	IF (@LOOP = 2)
	BEGIN
		CLOSE LOOP2
		DEALLOCATE LOOP2
	END
	IF (@ErrorCatched = 0)
	BEGIN
		ROLLBACK TRANSACTION T1
	END
END

IF (@Ignored = 1)
	UPDATE Int_ExecutionDetails SET Skipped = 1, RunTimeEnd = GETDATE(), ResultID = @ResultID, [Message] = @Message WHERE ID = @ProcessID;
ELSE IF (@IsNew = 1)
	UPDATE Int_ExecutionDetails SET Inserted = 1, RunTimeEnd = GETDATE(), ResultID = @ResultID, [Message] = @Message WHERE ID = @ProcessID;
ELSE
	UPDATE Int_ExecutionDetails SET Updated = 1, RunTimeEnd = GETDATE(), ResultID = @ResultID, [Message] = @Message WHERE ID = @ProcessID;


FETCH NEXT FROM LOOP1
INTO @OrderID,@OrderDate,@DeliveryDate,@LPO,@Status,@ShipToCode,@EmployeeCode;

END

CLOSE LOOP1
DEALLOCATE LOOP1

END