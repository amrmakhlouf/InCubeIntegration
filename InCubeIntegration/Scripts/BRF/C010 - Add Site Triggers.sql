IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'GetSAPOrderNumbers')
BEGIN
EXEC('CREATE TRIGGER [dbo].[GetSAPOrderNumbers] 
   ON  [dbo].[Int_ActionTrigger]
   AFTER UPDATE
AS 
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	IF ((SELECT ActionID from inserted) = 32)
	BEGIN

		UPDATE SO SET SO.ConfirmationSignature = STP.SAPNo
		FROM 
		(SELECT OrderNo, MIN(SUBSTRING(Description,7,10)) SAPNo FROM STPDetails 
		WHERE SUBSTRING(Description,6,1) = ''O'' AND SendingDate > DATEADD(dd,-2,GETDATE()) GROUP BY OrderNo) STP
		INNER JOIN SalesOrder SO ON SO.OrderID = STP.OrderNo 
		WHERE SO.ConfirmationSignature IS NULL

	END
    -- Insert statements for trigger here

END')
END

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'OrderStatusChanged')
BEGIN
EXEC('CREATE TRIGGER [dbo].[OrderStatusChanged] 
   ON  [dbo].[SalesOrder]
   AFTER UPDATE
AS 

declare
@orderid varchar(max),
@customerid int,
@OutletID int,
@OrganizationID int,
@EmployeeID int,
@OrderStatusID int,
@OrderSubStatusID int,
@NewSynch bit,
@OldSync bit

BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

DECLARE LOOP_Orders CURSOR FOR
	SELECT CustomerID, OrderID, OrderStatusID, OrganizationID, OrderSubStatusID, EmployeeID, OutletID, Synchronized FROM inserted;
OPEN LOOP_Orders
FETCH NEXT FROM LOOP_Orders INTO @customerid, @orderid, @OrderStatusID, @OrganizationID,@OrderSubStatusID,@EmployeeID,@OutletID,@NewSynch
WHILE(@@FETCH_STATUS=0)
BEGIN
select @OldSync = d.Synchronized from deleted d where OrderID = @orderid and CustomerID = @customerID
if (@OrderStatusID = 8 AND @OrderSubStatusID > 0)
	BEGIN
	insert into DeliveryAssignment (EmployeeID,OrderID,CustomerID,OutletID,OrganizationID,AssignmentDate,DeliveryAssignmentID,DeliveryStatusID)
	values (@EmployeeID,@orderid,@customerid,@OutletID,@OrganizationID,GETDATE(),(SELECT ISNULL(MAX(DeliveryAssignmentID),0)+1 FROM DeliveryAssignment),3) 
	END
    IF (@OrderStatusID = 12)
	BEGIN
	UPDATE SalesOrder SET OrderStatusID = 1 WHERE OrderID = @orderid AND CustomerID = @customerid
	END
	IF (@OldSync = 1 AND @NewSynch = 0)
	BEGIN
	INSERT INTO OrderSyncHistory (OrderID,SendingProcessID,CustomerID,AttemptDate,Result) VALUES (@orderid,-1,@customerid,GETDATE(),''ReSend'')
	UPDATE SalesOrder SET OrderStatusID = 2 WHERE OrderID = @orderid AND CustomerID = @customerid
	END
FETCH NEXT FROM LOOP_Orders INTO @customerid, @orderid, @OrderStatusID, @OrganizationID,@OrderSubStatusID,@EmployeeID,@OutletID,@NewSynch
END
CLOSE LOOP_Orders
DEALLOCATE LOOP_Orders

END
')
END

IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'UPDATE_DOCUMENT_SEQUENCE_TRG')
BEGIN
EXEC('CREATE TRIGGER [dbo].[UPDATE_DOCUMENT_SEQUENCE_TRG]
   ON  [dbo].[SalesOrder]
   AFTER INSERT
AS 
declare @description varchar(max),
@orderid varchar(max),
@employeeid int,
@CreationSource int,
@customerid int

BEGIN
SET NOCOUNT ON;

 select @customerid=i.CustomerID, @description = i.Description,@orderid = i.OrderID,@employeeid = i.EmployeeID,@CreationSource =  isnull(i.CreationSource,0) from inserted i;


 if(@description <> ''Panda Ord'' and @CreationSource=2)
 begin
 update DocumentSequence set MaxTransactionOrderID = @orderid where Employeeid = @employeeid and MaxTransactionOrderID< @orderid;
 update SalesOrder set OrderStatusID = 2 where OrderID = @orderid and CustomerID = @customerid
 end

END')
END
