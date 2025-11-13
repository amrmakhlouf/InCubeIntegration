--Create table (InvoiceLog)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'InvoiceLog') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE InvoiceLog(
      [Parameters] NVARCHAR(200) NULL ,
      [ErrorNumber] NVARCHAR(200) NULL ,
      [ErrorLine] NVARCHAR(200) NULL ,
      [ErrorMessage] NVARCHAR(200) NULL ,
      [ErrorTime] DATETIME NULL 
 );

PRINT 'Table InvoiceLog added successfully.'
END
ELSE
PRINT 'Table InvoiceLog already Exists.'


--sp_UpdateInvoices
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_UpdateInvoices')
BEGIN

EXEC('
CREATE PROCEDURE sp_UpdateInvoices
(
@TriggerID INT,
@TerritoryID INT,
@EmployeeID INT,
@OrganizationID INT
)
AS

BEGIN

DECLARE 
@Query nvarchar(3000)

SELECT @Query = ''
DECLARE 
@Inserted INT,
@Updated INT,
@CustomerID INT,
@OutletID INT,
@TransactionID nvarchar(50),
@Balance decimal(19,3),
@RemainingAmount decimal(19,3),
@Note nvarchar(100),
@SourceTransactionID nvarchar(100),
@DueDate datetime,
@InvoiceDate datetime,
@TransactionType INT

SET @Inserted = 0
SET @Updated = 0 

DECLARE LOOP1 CURSOR FOR
SELECT DISTINCT C.SAPAccountingDocument+'''':''''+C.ItemNumber, CO.CustomerID, CO.OutletID, C.Balance, C.RemainingAmount
, C.SAPBillingNumber, C.RVNumber, C.DueDate, C.InvDate
FROM CNT'' + CAST(@TriggerID AS nvarchar(10)) + '' C
INNER JOIN customeroutlet CO ON CO.CustomerCode = CAST(CAST(C.CustomerCode AS INT) AS nvarchar(10))
INNER JOIN CustOutTerritory COT ON COT.CustomerID = CO.CustomerID AND COT.OutletID = CO.OutletID
WHERE TerritoryID = '' + CAST(@TerritoryID AS nvarchar(10)) + ''

OPEN LOOP1
FETCH NEXT FROM LOOP1
INTO @TransactionID,@CustomerID,@OutletID,@Balance,@RemainingAmount,@Note,@SourceTransactionID,@DueDate,@InvoiceDate
WHILE(@@FETCH_STATUS=0)
BEGIN

BEGIN TRY

SET @TransactionType = (CASE WHEN @RemainingAmount < 0 THEN 5 ELSE 1 END)
SET @RemainingAmount = ABS(@RemainingAmount)

IF NOT EXISTS (SELECT * FROM [Transaction] WHERE TransactionID = @TransactionID AND CustomerID = @CustomerID AND OutletID = @OutletID)
BEGIN

INSERT INTO [Transaction] (TransactionID,CustomerID,OutletID,EmployeeID,TransactionDate,DueDate,TransactionTypeID,Discount,Synchronized
,RemainingAmount,GrossTotal,NetTotal,Notes,TransactionStatusID,Posted,Voided,DivisionID,SalesMode,OrganizationID,RouteID,CreatedDate
,SourceTransactionID)
VALUES (@TransactionID,@CustomerID,@OutletID,'' + CAST(@EmployeeID AS nvarchar(10)) + '',@InvoiceDate,@DueDate,@TransactionType,0,1
,@RemainingAmount,@RemainingAmount,@RemainingAmount,@Note,1,1,0,-1,2,'' + CAST(@OrganizationID AS nvarchar(10)) + '','' 
+ CAST(@TriggerID AS nvarchar(10)) + '',GETDATE(),@SourceTransactionID)

SET @Inserted = @Inserted + 1

END
ELSE BEGIN

UPDATE [Transaction] SET RemainingAmount = @RemainingAmount, GrossTotal = @RemainingAmount, NetTotal = @RemainingAmount
, TransactionDate = @InvoiceDate, DueDate = @DueDate, TransactionTypeID = @TransactionType, Notes = @Note, EmployeeID = '' 
+ CAST(@EmployeeID AS nvarchar(10)) + '',Synchronized = 1, UpdatedDate = GETDATE(), SourceTransactionID = @SourceTransactionID
WHERE TransactionID = @TransactionID AND CustomerID = @CustomerID AND OutletID = @OutletID

SET @Updated = @Updated + 1

END

UPDATE Account SET Balance = @Balance WHERE AccountID IN (
SELECT AccountID FROM AccountCust WHERE CustomerID = @CustomerID
UNION
SELECT AccountID FROM AccountCustOut WHERE CustomerID = @CustomerID AND OutletID = @OutletID)

END TRY
BEGIN CATCH

INSERT INTO InvoiceLog
SELECT 
@TransactionID + '''':'' + CAST(@TriggerID AS nvarchar(10)) + ''''''
,ERROR_NUMBER() AS ErrorNumber
,ERROR_LINE() AS ErrorLine
,ERROR_MESSAGE() AS ErrorMessage
,GETDATE();
END CATCH

FETCH NEXT FROM LOOP1
INTO @TransactionID,@CustomerID,@OutletID,@Balance,@RemainingAmount,@Note,@SourceTransactionID,@DueDate,@InvoiceDate

END
CLOSE LOOP1
DEALLOCATE LOOP1

UPDATE [Transaction] SET TransactionDate = GETDATE() WHERE TransactionDate > GETDATE();

SELECT @Inserted Inserted, @Updated Updated 
''

EXEC (@Query)

END
')

END


--InsertATMTable
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'InsertATMTable')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[InsertATMTable]
    @myTableType ATM_Table readonly
AS
BEGIN
    insert into [dbo].ATM_Deposite select * from @myTableType 
END
')

END


--sp_InsertVolumeTargets
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_InsertVolumeTargets')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_InsertVolumeTargets]
(
@Month INT,
@Year INT
)
AS

BEGIN
DECLARE @FromDate datetime, @ToDate datetime
SET @FromDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month AS nvarchar(2)) + ''-1 00:00:00'',102)
SET @ToDate = DATEADD(MM,1,@FromDate)
SET @ToDate = DATEADD(SS,-1,@ToDate)

INSERT INTO AchievementTargetCustomer (TargetID, AchievementID, CustomerID, OutletID, Value, ItemID, PackTypeID, FromDate, ToDate, Value2)
SELECT TargetID,58,CustomerID,OutletID,Quantity/EquivalencyFactor,ItemID,PackTypeID,@FromDate,@ToDate,Value/EquivalencyFactor 
FROM VolumeTargets WHERE ItemID IS NOT NULL AND CustomerID IS NOT NULL

END
')

END


--sp_DeleteVolumeTargets
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_DeleteVolumeTargets')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_DeleteVolumeTargets]
(
@Month INT,
@Year INT
)
AS
BEGIN
DECLARE @FromDate datetime, @ToDate datetime
SET @FromDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month AS nvarchar(2)) + ''-1 00:00:00'',102)
SET @ToDate = DATEADD(MM,1,@FromDate)
SET @ToDate = DATEADD(SS,-1,@ToDate)

DELETE FROM AchievementTargetCustomer WHERE AchievementID = 58 AND FromDate >= @FromDate AND ToDate <= @ToDate
DELETE FROM AchievementCustomer WHERE AchievementID = 58 AND FromDate >= @FromDate AND ToDate <= @ToDate
DELETE FROM AchievementCustomerDetail WHERE AchievementID = 58 AND DocumentDate >= @FromDate AND DocumentDate <= @ToDate
END
')

END


--sp_PrepareTargetsData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_PrepareTargetsData')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_PrepareTargetsData]
AS
BEGIN

UPDATE VolumeTargets SET CustomerID = TBL.CustomerID, OutletID = TBL.OutletID, ItemID = TBL.ItemID, PackTypeID = TBL.PackTypeID,
EquivalencyFactor = TBL.EquivalencyFactor, TargetID = TBL.TargetID, Result = TBL.Result
FROM
(SELECT TBL1.*, RANK() OVER (PARTITION BY TBL1.CustomerID,TBL1.OutletID ORDER BY TBL1.ItemID DESC) TargetID
FROM
(SELECT VT.ID, CO.CustomerID, CO.OutletID, I.ItemID, P.PackTypeID,
CASE PTL.Description WHEN ''CX'' THEN (CASE VT.UOM WHEN ''Kg'' THEN (CASE P.Weight WHEN 0 THEN 1 ELSE P.Weight END) ELSE 1 END) ELSE 1 END EquivalencyFactor,
(CASE WHEN I.ItemID IS NULL THEN ''Item code not defined'' ELSE
(CASE WHEN CO.CustomerID IS NULL THEN ''Customer code not defined'' ELSE
(CASE WHEN P.PackID IS NULL THEN ''Item has no pack'' ELSE ''Success'' END) END) END) Result
FROM VolumeTargets VT
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = VT.CustomerCode
LEFT JOIN Item I ON I.ItemCode = VT.ItemCode
LEFT JOIN Pack P ON P.ItemID = I.ItemID
LEFT JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1) TBL1) TBL
WHERE VolumeTargets.ID = TBL.ID

END
')

END


--sp_InsertPendingSTP
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_InsertPendingSTP')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_InsertPendingSTP]
AS
BEGIN

INSERT INTO PendingSTP
SELECT E.Email,SO.OrderID,GETDATE() FROM
(
SELECT OrderID FROM SalesOrder WHERE OrderDate > CONVERT(datetime, CAST(DATEPART(DD,GETDATE()) AS nvarchar(2)) + ''/'' + CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + ''/'' + CAST(DATEPART(YYYY,GETDATE()) AS nvarchar(4)), 103) AND Synchronized = 1
EXCEPT
(SELECT OrderNo FROM STPDetails WHERE ReadingDate > CONVERT(datetime, CAST(DATEPART(DD,GETDATE()) AS nvarchar(2)) + ''/'' + CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + ''/'' + CAST(DATEPART(YYYY,GETDATE()) AS nvarchar(4)), 103)
UNION
SELECT DISTINCT OrderNo FROM PendingSTP)
) TBL
INNER JOIN SalesOrder SO ON TBL.OrderID = SO.OrderID
INNER JOIN Employee E ON SO.EmployeeID = E.EmployeeID

END
')

END

--sp_CalculateRoutePlanAchievements
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_CalculateRoutePlanAchievements')
BEGIN

EXEC('
CREATE PROCEDURE sp_CalculateRoutePlanAchievements (@Day INT, @Month INT, @Year INT, @EmployeeID INT)
AS
BEGIN

DECLARE
@FromDate DateTime,
@ToDate DateTime,
@TerritoryID INT

END
')

END

--sp_CalculateActiveCustomersAchievements
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_CalculateActiveCustomersAchievements')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_CalculateActiveCustomersAchievements] (@Month INT, @Year INT, @EmployeeID INT)
AS
BEGIN

DECLARE
@FromDate DateTime,
@ToDate DateTime,
@TerritoryID INT

SET @TerritoryID = (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID = @EmployeeID)
SET @FromDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month AS nvarchar(2)) + ''-1 00:00:00'', 120)
SET @ToDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month+1 AS nvarchar(2)) + ''-1 00:00:00'', 120)
SET @ToDate = DATEADD(ss,-1,@ToDate)

--Delete Calculated Targets and Achievements
DELETE FROM AchievementActiveCustItem WHERE AchievementActiveCustomerID IN 
(SELECT AchievementActiveCustomerID FROM AchievementActiveCustomer WHERE EmployeeID = @EmployeeID AND AchievementID = 48 AND AchievementOnPeriodID IN (
SELECT AchievementOnPeriodID FROM AchievementEmployee WHERE EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate AND AchievementID = 48))

DELETE FROM AchievementActiveCustomer WHERE EmployeeID = @EmployeeID AND AchievementID = 48 AND AchievementOnPeriodID IN (
SELECT AchievementOnPeriodID FROM AchievementEmployee WHERE EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate AND AchievementID = 48)

DELETE FROM AchievementActiveDivCat WHERE EmployeeID = @EmployeeID AND AchievementID = 48 AND AchievementOnPeriodID IN (
SELECT AchievementOnPeriodID FROM AchievementEmployee WHERE EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate AND AchievementID = 48)

DELETE FROM AchievementTargetEmployee WHERE EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate AND AchievementID = 48

DELETE FROM AchievementEmployee WHERE EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate AND AchievementID = 48

--Employee Target
INSERT INTO AchievementTargetEmployee (TargetID,AchievementID,EmployeeID,Value,ItemID,FromDate,ToDate,BrandID,
PackTypeID,PackGroupID,DivisionID,ItemCategoryID,TargetPoints,Value2)
SELECT (SELECT ISNULL(MAX(TargetID),0)+1 FROM AchievementTargetEmployee WHERE AchievementID = 48)
,48,@EmployeeID,COUNT(*),-1,@FromDate,@ToDate,-1,-1,-1,-1,-1,0,0
FROM CustOutTerritory WHERE TerritoryID = @TerritoryID

--Active Customers
INSERT INTO AchievementActiveCustomer (AchievementActiveCustomerID,AchievementOnPeriodID,AchievementID,
EmployeeID,CustomerID,OutletID)
SELECT (SELECT ISNULL(MAX(AchievementActiveCustomerID),0) FROM AchievementActiveCustomer) 
+ RANK() OVER (PARTITION BY 1 ORDER BY AC.CustomerID), 
(SELECT TargetID FROM AchievementTargetEmployee WHERE AchievementID = 48 
AND FromDate = @FromDate AND ToDate = @ToDate AND EmployeeID = @EmployeeID),
48,@EmployeeID, AC.CustomerID,AC.OutletID
FROM AchievementCustomer AC
INNER JOIN CustOutTerritory COT ON COT.CustomerID = AC.CustomerID AND COT.OutletID = AC.OutletID
WHERE COT.TerritoryID = @TerritoryID AND (AC.Value > 0 OR AC.Value2 > 0) 
AND AC.FromDate = @FromDate AND AC.ToDate = @ToDate
GROUP BY AC.CustomerID,AC.OutletID

--Active Items
INSERT INTO AchievementActiveCustItem (AchievementActiveCustomerID,DivisionID,ItemCategoryID,ItemID)
SELECT DISTINCT AAC.AchievementActiveCustomerID,IC.DivisionID,I.ItemCategoryID,AC.ItemID
FROM AchievementCustomer AC
INNER JOIN AchievementActiveCustomer AAC ON AAC.CustomerID = AC.CustomerID AND AAC.OutletID = AC.OutletID
AND AAC.AchievementOnPeriodID IN (SELECT TargetID FROM AchievementTargetEmployee WHERE AchievementID = 48 
AND EmployeeID = @EmployeeID AND FromDate = @FromDate AND ToDate = @ToDate)
INNER JOIN Item I ON I.ItemID = AC.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
WHERE AAC.AchievementID = 48 AND AAC.EmployeeID = @EmployeeID AND (AC.Value > 0 OR AC.Value2 > 0) 
AND AC.FromDate = @FromDate AND AC.ToDate = @ToDate


--Active Div and Cat
INSERT INTO  AchievementActiveDivCat (AchievementOnPeriodID,AchievementID,EmployeeID,DivisionID,
ItemCategoryID,NumberOfSoldCustomers)
SELECT AAC.AchievementOnPeriodID,48,@EmployeeID,AACI.DivisionID,AACI.ItemCategoryID,COUNT(DISTINCT AAC.CustomerID)
FROM AchievementActiveCustItem AACI
INNER JOIN AchievementActiveCustomer AAC ON AAC.AchievementActiveCustomerID = AACI.AchievementActiveCustomerID
INNER JOIN AchievementTargetEmployee ATE ON ATE.TargetID = AAC.AchievementOnPeriodID 
AND ATE.EmployeeID = AAC.EmployeeID AND ATE.AchievementID = AAC.AchievementID
WHERE ATE.AchievementID = 48 AND ATE.EmployeeID = @EmployeeID
AND ATE.FromDate = @FromDate AND ATE.ToDate = @ToDate
GROUP BY AAC.AchievementOnPeriodID,AACI.DivisionID,AACI.ItemCategoryID
UNION
SELECT AAC.AchievementOnPeriodID,48,@EmployeeID,AACI.DivisionID,-1,COUNT(DISTINCT AAC.CustomerID)
FROM AchievementActiveCustItem AACI
INNER JOIN AchievementActiveCustomer AAC ON AAC.AchievementActiveCustomerID = AACI.AchievementActiveCustomerID
INNER JOIN AchievementTargetEmployee ATE ON ATE.TargetID = AAC.AchievementOnPeriodID 
AND ATE.EmployeeID = AAC.EmployeeID AND ATE.AchievementID = AAC.AchievementID
WHERE ATE.AchievementID = 48 AND ATE.EmployeeID = @EmployeeID
AND ATE.FromDate = @FromDate AND ATE.ToDate = @ToDate
GROUP BY AAC.AchievementOnPeriodID,AACI.DivisionID

--Achievement Employee
INSERT INTO AchievementEmployee (AchievementOnPeriodID,AchievementID,EmployeeID,FromDate,ToDate,Value,
TargetValue,TargetID,ItemID,BrandID,PackTypeID,PackGroupID,DivisionID,ItemCategoryID)
SELECT ATE.TargetID,48,@EmployeeID,ATE.FromDate,ATE.ToDate,(SELECT COUNT(*) FROM AchievementActiveCustomer 
WHERE AchievementOnPeriodID = ATE.TargetID AND AchievementID = 48 AND EmployeeID = @EmployeeID) Value, ATE.Value,
ATE.TargetID,-1,-1,-1,-1,-1,-1
FROM AchievementTargetEmployee ATE
WHERE ATE.FromDate = @FromDate AND ATE.ToDate = @ToDate 
AND ATE.EmployeeID = @EmployeeID AND ATE.AchievementID = 48

END
')

END

--sp_CalculateVolumeAchievements
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_CalculateVolumeAchievements')
BEGIN

EXEC('
CREATE PROCEDURE sp_CalculateVolumeAchievements (@Month INT, @Year INT, @EmployeeID INT)
AS
BEGIN

DECLARE
@FromDate DateTime,
@ToDate DateTime,
@TerritoryID INT,
@DaysInMonth INT

SET @TerritoryID = (SELECT TerritoryID FROM EmployeeTerritory WHERE EmployeeID = @EmployeeID)
SET @FromDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month AS nvarchar(2)) + ''-1 00:00:00'', 120)
SET @ToDate = CONVERT(datetime, CAST(@Year AS nvarchar(4)) + ''-'' + CAST(@Month+1 AS nvarchar(2)) + ''-1 00:00:00'', 120)
SET @ToDate = DATEADD(ss,-1,@ToDate)
SET @DaysInMonth = DATEPART(dd,@ToDate)
--======================================================================================================
--======================================================================================================
--Insert zero targets for items and customers that have sales but no target
--======================================================================================================
--======================================================================================================
INSERT INTO AchievementTargetCustomer (CustomerID,OutletID,ItemID,PackTypeID,TargetID,AchievementID,Value,
Value2,FromDate,ToDate,IsPromoted,PackGroupID)

SELECT CustomerID,OutletID,ItemID,PackTypeID, 
(SELECT ISNULL(MAX(TargetID),0) FROM AchievementTargetCustomer 
WHERE AchievementID = 58) + RANK() OVER (PARTITION BY TBL.CustomerID,TBL.OutletID ORDER BY TBL.ItemID DESC) TargetID,
58,0,0,@FromDate,@ToDate,0,-1
FROM (
SELECT TD.CustomerID,TD.OutletID,P.ItemID,P.PackTypeID
FROM TransactionDetail TD
INNER JOIN [Transaction] TR ON TR.TransactionID = TD.TransactionID AND TR.CustomerID = TD.CustomerID
AND TR.OutletID = TD.OutletID AND TR.DivisionID = TD.DivisionID
INNER JOIN CustOutTerritory COT ON COT.CustomerID = TR.CustomerID AND COT.OutletID = TR.OutletID
INNER JOIN Pack P ON P.PackID = TD.PackID
WHERE TD.SalesTransactionTypeID = 1 AND TR.Voided <> 1 AND TR.Posted = 1 
AND TR.TransactionDate >= @FromDate AND TR.TransactionDate <= @ToDate 
AND COT.TerritoryID = @TerritoryID
EXCEPT
SELECT ATC.CustomerID,ATC.OutletID,ATC.ItemID,ATC.PackTypeID
FROM AchievementTargetCustomer ATC
INNER JOIN CustOutTerritory COT ON COT.CustomerID = ATC.CustomerID AND COT.OutletID = ATC.OutletID
WHERE ATC.AchievementID = 58 AND ATC.FromDate >= @FromDate 
AND ATC.ToDate <= @ToDate AND COT.TerritoryID = @TerritoryID
) TBL

--======================================================================================================
--======================================================================================================
--Delete Calculated Achievements
--======================================================================================================
--======================================================================================================
--Daily Targets
DELETE ATC
FROM AchievementTargetCustomer ATC 
INNER JOIN CustOutTerritory COT ON COT.CustomerID = ATC.CustomerID AND COT.OutletID = ATC.OutletID
WHERE AchievementID = 56 AND ATC.FromDate = @FromDate AND ATC.ToDate = @ToDate AND COT.TerritoryID = @TerritoryID

--Achievements
DELETE AC
FROM AchievementCustomer AC 
INNER JOIN CustOutTerritory COT ON COT.CustomerID = AC.CustomerID AND COT.OutletID = AC.OutletID
WHERE AchievementID = 58 AND AC.FromDate = @FromDate AND AC.ToDate = @ToDate AND COT.TerritoryID = @TerritoryID

--Employee Targets
DELETE FROM AchievementTargetEmployee
WHERE AchievementID = 58 AND FromDate = @FromDate AND ToDate = @ToDate AND EmployeeID = @EmployeeID

--Employee Achievement
DELETE FROM AchievementEmployee
WHERE AchievementID = 58 AND FromDate = @FromDate AND ToDate = @ToDate AND EmployeeID = @EmployeeID

--======================================================================================================
--======================================================================================================
--Calculate Customers Achievements
--======================================================================================================
--======================================================================================================
INSERT INTO AchievementCustomer (AchievementOnPeriodID,AchievementID,CustomerID,OutletID,FromDate,ToDate,
Value,TargetValue,TargetID,ItemID,PackTypeID,Value2,Target2Value,PackGroupID)

SELECT ATC.TargetID,58,ATC.CustomerID,ATC.OutletID,ATC.FromDate,ATC.ToDate,ISNULL(SUM(TD.Quantity),0),ATC.Value,
ATC.TargetID,ATC.ItemID, ATC.PackTypeID, ISNULL(SUM(TD.Quantity*TD.Price-TD.Discount),0),ATC.Value2,-1
FROM 
AchievementTargetCustomer ATC 
INNER JOIN CustOutTerritory COT ON COT.CustomerID = ATC.CustomerID AND COT.OutletID = ATC.OutletID
INNER JOIN Pack P ON P.ItemID = ATC.ItemID AND P.PackTypeID = ATC.PackTypeID
LEFT JOIN [Transaction] TR ON TR.CustomerID = ATC.CustomerID AND TR.OutletID = ATC.OutletID
AND TR.TransactionDate >= @FromDate AND TR.TransactionDate <= @ToDate
AND TR.Voided <> 1 AND TR.Posted = 1 AND TR.EmployeeID = @EmployeeID
LEFT JOIN TransactionDetail TD ON TR.TransactionID = TD.TransactionID AND TR.CustomerID = TD.CustomerID
AND TR.OutletID = TD.OutletID AND TR.DivisionID = TD.DivisionID AND TD.PackID = P.PackID
WHERE ATC.AchievementID = 58 AND ATC.FromDate >= @FromDate AND ATC.ToDate <= @ToDate
AND COT.TerritoryID = @TerritoryID
GROUP BY ATC.CustomerID,ATC.OutletID,ATC.FromDate,ATC.ToDate,ATC.Value,ATC.TargetID,ATC.ItemID, 
ATC.PackTypeID,ATC.Value2

--======================================================================================================
--======================================================================================================
--Calculate Employee Achievements
--======================================================================================================
--======================================================================================================
--Item Level
INSERT INTO AchievementEmployee (AchievementOnPeriodID,AchievementID,EmployeeID,FromDate,ToDate,Value,
TargetValue,TargetID,ItemID,BrandID,PackTypeID,DivisionID,ItemCategoryID,Value2,Target2Value)
SELECT (SELECT ISNULL(MAX(AchievementOnPeriodID),0) FROM AchievementEmployee WHERE AchievementID = 58) + 
RANK() OVER (PARTITION BY AC.FromDate,AC.ToDate ORDER BY AC.ItemID DESC),
58,@EmployeeID,AC.FromDate,AC.ToDate,SUM(AC.Value),SUM(AC.TargetValue),1,AC.ItemID,-1,AC.PackTypeID,
IC.DivisionID,I.ItemCategoryID,SUM(AC.Value2),SUM(AC.Target2Value)
FROM AchievementCustomer AC
INNER JOIN CustOutTerritory COT ON COT.CustomerID = AC.CustomerID AND COT.OutletID = AC.OutletID
INNER JOIN Item I ON I.ItemID = AC.ItemID
INNER JOIN ItemCategory IC ON IC.ItemCategoryID = I.ItemCategoryID
WHERE AC.AchievementID = 58 AND AC.FromDate >= @FromDate 
AND AC.ToDate <= @ToDate AND COT.TerritoryID = @TerritoryID
GROUP BY AC.ItemID,AC.PackTypeID,I.ItemCategoryID,IC.DivisionID,AC.FromDate,AC.ToDate

--Category Level
INSERT INTO AchievementEmployee (AchievementOnPeriodID,AchievementID,EmployeeID,FromDate,ToDate,Value,
TargetValue,TargetID,ItemID,BrandID,PackTypeID,DivisionID,ItemCategoryID,Value2,Target2Value)
SELECT (SELECT ISNULL(MAX(AchievementOnPeriodID),0) FROM AchievementEmployee WHERE AchievementID = 58) + 
RANK() OVER (PARTITION BY AE.FromDate,AE.ToDate ORDER BY AE.ItemCategoryID DESC),
58,@EmployeeID,AE.FromDate,AE.ToDate,SUM(AE.Value),SUM(AE.TargetValue),1,-1,-1,-1,
AE.DivisionID,AE.ItemCategoryID,SUM(AE.Value2),SUM(AE.Target2Value)
FROM AchievementEmployee AE
WHERE AE.AchievementID = 58 AND AE.FromDate >= @FromDate 
AND AE.ToDate <= @ToDate AND AE.EmployeeID = @EmployeeID AND AE.ItemID <> -1
GROUP BY AE.ItemCategoryID,AE.DivisionID,AE.FromDate,AE.ToDate

--Division Level
INSERT INTO AchievementEmployee (AchievementOnPeriodID,AchievementID,EmployeeID,FromDate,ToDate,Value,
TargetValue,TargetID,ItemID,BrandID,PackTypeID,DivisionID,ItemCategoryID,Value2,Target2Value)
SELECT (SELECT ISNULL(MAX(AchievementOnPeriodID),0) FROM AchievementEmployee WHERE AchievementID = 58) + 
RANK() OVER (PARTITION BY AE.FromDate,AE.ToDate ORDER BY AE.DivisionID DESC),
58,@EmployeeID,AE.FromDate,AE.ToDate,SUM(AE.Value),SUM(AE.TargetValue),1,-1,-1,-1,
AE.DivisionID,-1,SUM(AE.Value2),SUM(AE.Target2Value)
FROM AchievementEmployee AE
WHERE AE.AchievementID = 58 AND AE.FromDate >= @FromDate 
AND AE.ToDate <= @ToDate AND AE.EmployeeID = @EmployeeID AND AE.ItemID <> -1
GROUP BY AE.DivisionID,AE.FromDate,AE.ToDate

--Employee Level
INSERT INTO AchievementEmployee (AchievementOnPeriodID,AchievementID,EmployeeID,FromDate,ToDate,Value,
TargetValue,TargetID,ItemID,BrandID,PackTypeID,DivisionID,ItemCategoryID,Value2,Target2Value)
SELECT (SELECT ISNULL(MAX(AchievementOnPeriodID),0)+1 FROM AchievementEmployee WHERE AchievementID = 58)
,58,@EmployeeID,AE.FromDate,AE.ToDate,SUM(AE.Value),SUM(AE.TargetValue),1,-1,-1,-1,
-1,-1,SUM(AE.Value2),SUM(AE.Target2Value)
FROM AchievementEmployee AE
WHERE AE.AchievementID = 58 AND AE.FromDate >= @FromDate 
AND AE.ToDate <= @ToDate AND AE.EmployeeID = @EmployeeID AND AE.ItemID <> -1
GROUP BY AE.FromDate,AE.ToDate

--Targets
INSERT INTO AchievementTargetEmployee 
(TargetID,AchievementID,EmployeeID,Value,ItemID,FromDate,ToDate,BrandID,PackTypeID,PackGroupID,DivisionID,
ItemCategoryID,TargetPoints,Value2)
SELECT AchievementOnPeriodID,58,@EmployeeID,TargetValue,ItemID,FromDate,ToDate,-1,PackTypeID,-1,DivisionID,ItemCategoryID,0,Target2Value
FROM AchievementEmployee AE WHERE AE.EmployeeID = @EmployeeID AND AE.FromDate >= @FromDate 
AND AE.ToDate <= @ToDate AND AE.AchievementID = 58


--======================================================================================================
--======================================================================================================
--Calculate Daily Targets
--======================================================================================================
--======================================================================================================
INSERT INTO AchievementTargetCustomer (TargetID,AchievementID,CustomerID,OutletID,Value,ItemID,FromDate,ToDate,IsPromoted,PackTypeID,Value2)
SELECT ATC.TargetID,56 AchievementID, ATC.CustomerID, ATC.OutletID,
CASE WHEN (ATC.Value - ISNULL(AC.Value,0)) > 0 THEN ROUND((ISNULL(ATC.Value,0) - ISNULL(AC.Value,0)) / ISNULL(CustomersVisits.RemainingVisits,1),0) ELSE 0 END Value,
ATC.ItemID, ATC.FromDate, ATC.ToDate, ATC.IsPromoted, ATC.PackTypeID,
CASE WHEN (ATC.Value2 - ISNULL(AC.Value2,0)) > 0 THEN ROUND((ISNULL(ATC.Value2,0) - ISNULL(AC.Value2,0)) / ISNULL(CustomersVisits.RemainingVisits,1),2) ELSE 0 END Value2

FROM AchievementTargetCustomer ATC 

LEFT OUTER JOIN AchievementCustomer AC 
ON ATC.AchievementID = AC.AchievementID AND ATC.CustomerID = AC.CustomerID AND ATC.OutletID = AC.OutletID AND ATC.FromDate = AC.FromDate AND ATC.ToDate = AC.ToDate AND ATC.ItemID = AC.ItemID
and AC.PackTypeID=ATC.PackTypeID 
LEFT OUTER JOIN
(SELECT RC.CustomerID, RC.OutletID, COUNT(DISTINCT Days.Date) RemainingVisits
FROM RouteCustomer RC
INNER JOIN

(SELECT ET.EmployeeID, RVP.*, (SELECT COUNT(*) FROM RouteVisitPattern WHERE RouteID = RVP.RouteID) RouteRepeat
FROM RouteVisitPattern RVP
INNER JOIN Route R ON RVP.RouteID = R.RouteID
INNER JOIN EmployeeTerritory ET ON R.TerritoryID = ET.TerritoryID
) Routes
ON RC.RouteID = Routes.RouteID

INNER JOIN
(SELECT CONVERT(date,CAST(DayNum AS nvarchar(2)) + ''/'' + CAST(@Month as nvarchar(2)) + ''/'' + CAST(@Year as nvarchar(4)),103) Date, 
DATEPART(WW,CONVERT(date,CAST(DayNum AS nvarchar(2)) + ''/'' + CAST(@Month as nvarchar(2)) + ''/'' + CAST(@Year as nvarchar(4)),103)) WeekOfYear,
DATENAME(DW,CONVERT(date,CAST(DayNum AS nvarchar(2)) + ''/'' + CAST(@Month as nvarchar(2)) + ''/'' + CAST(@Year as nvarchar(4)),103)) DayOfWeek
FROM MonthDays WHERE DayNum >= DATEPART(dd,GETDATE()) AND DayNum <= @DaysInMonth
) Days
ON CASE (Days.WeekOfYear%Routes.RouteRepeat) WHEN 0 THEN Routes.RouteRepeat ELSE Days.WeekOfYear%Routes.RouteRepeat END = Routes.Week

LEFT OUTER JOIN
(SELECT EmployeeID,CONVERT(date,CAST(Day AS nvarchar(2)) + + ''/'' + CAST(@Month as nvarchar(2)) + ''/'' + CAST(@Year as nvarchar(4)),103) Date 
FROM CalendarEvents WHERE Year = @Year AND Month = @Month) Vacations
ON Days.Date = Vacations.Date AND (Routes.EmployeeID = Vacations.EmployeeID OR Vacations.EmployeeID = -1)

WHERE ((Days.DayOfWeek = ''Saturday'' AND Routes.Saturday = 1)
OR (Days.DayOfWeek = ''Sunday'' AND Routes.Sunday = 1)
OR (Days.DayOfWeek = ''Monday'' AND Routes.Monday = 1)
OR (Days.DayOfWeek = ''Tuesday'' AND Routes.Tuesday = 1)
OR (Days.DayOfWeek = ''Wednesday'' AND Routes.Wednesday = 1)
OR (Days.DayOfWeek = ''Thursday'' AND Routes.Thursday = 1)
OR (Days.DayOfWeek = ''Friday'' AND Routes.Friday = 1))

AND Vacations.EmployeeID IS NULL
GROUP BY RC.CustomerID, RC.OutletID) CustomersVisits
ON ATC.CustomerID = CustomersVisits.CustomerID AND ATC.OutletID = CustomersVisits.OutletID
INNER JOIN CustOutTerritory COT ON COT.CustomerID = ATC.CustomerID AND COT.OutletID = ATC.OutletID
WHERE ATC.AchievementID = 58 AND ATC.FromDate >= @FromDate AND ATC.ToDate <= @ToDate
AND COT.TerritoryID = @TerritoryID

END
')

END

--sp_CreateDummyTransactions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_CreateDummyTransactions')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_CreateDummyTransactions] (@EmployeeID INT)
AS
BEGIN
INSERT INTO TransactionDetail (TransactionID,CustomerID,OutletID,PackID,Quantity,Price,BasePrice,Tax,BaseTax,Discount,BaseDiscount,SalesTransactionTypeID,BatchNo,PackStatusID,ExpiryDate,ReturnReason,DivisionID)
SELECT CAST(HD.EmployeeID AS nvarchar(3)) + ''-'' + CAST(HD.Year AS nvarchar(4)) + ''-'' + CAST(HD.Month AS nvarchar(2)) + ''-'' + CAST(HD.CustomerID AS nvarchar(6)) + ''-'' + CAST(HD.OutletID AS nvarchar(2)) TransactionID,
HD.CustomerID, HD.OutletID, HD.PackID, HD.Quantity, HD.Price, HD.Price BasePrice, 0 Tax, 0 BaseTax, 0 Discount, 0 BaseDiscount, 1 SalesTransactionTypeID,
CAST(HD.Month AS nvarchar(2)) + CAST(HD.Year AS nvarchar(4)) BatchNo, -1 PackStatusID, 
CONVERT(datetime, ''01-01-1990'', 103) ExpiryDate, -1 ReturnReason, -1 DivisionID
FROM HistoryData HD WHERE HD.Quantity > 0 AND HD.EmployeeID = @EmployeeID
AND CAST(Month AS nvarchar(2))+CAST(Year as nvarchar(4)) IN (CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + CAST(DATEPART(YYYY,GETDATE()) AS nvarchar(4)),
CAST(DATEPART(MM,DATEADD(MM,-1,GETDATE())) AS nvarchar(2)) + CAST(DATEPART(YYYY,DATEADD(MM,-1,GETDATE())) AS nvarchar(4)))

INSERT INTO [Transaction] (TransactionID,TransactionDate,CustomerID,OutletID,EmployeeID,TransactionTypeID,Discount,Synchronized,RemainingAmount,GrossTotal,Voided,NetTotal,Tax,Posted,DivisionID,CreationReason,CreatedDate)
SELECT TRD.TransactionID,CONVERT(datetime, ''1/'' + SUBSTRING(TRD.BatchNo,1,LEN(TRD.BatchNo)-4) + ''/'' + SUBSTRING(TRD.BatchNo,LEN(TRD.BatchNo)-3,4),103) TransactionDate,TRD.CustomerID,TRD.OutletID,@EmployeeID, 1 TransactionTypeID, SUM(TRD.Discount) Discount, 1 Synchronized, 0 RemainingAmount,SUM(TRD.Quantity * TRD.Price) GrossTotal,
0 Voided, SUM(TRD.Quantity * TRD.Price - TRD.Discount + TRD.Tax) NetTotal, SUM(TRD.Tax) Tax,1 Posted,-1 DivisionID, TRD.BatchNo CreationReason, GETDATE() CreatedDate
FROM TransactionDetail TRD
WHERE TRD.BatchNo IN (CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + CAST(DATEPART(YYYY,GETDATE()) AS nvarchar(4)),
CAST(DATEPART(MM,DATEADD(MM,-1,GETDATE())) AS nvarchar(2)) + CAST(DATEPART(YYYY,DATEADD(MM,-1,GETDATE())) AS nvarchar(4)))
AND CAST(SUBSTRING(TRD.TransactionID,1,CHARINDEX(''-'',TransactionID)-1) AS INT) = @EmployeeID
GROUP BY TRD.TransactionID,TRD.CustomerID,TRD.OutletID,TRD.BatchNo

END
')

END

--sp_InsertHistoricalData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_InsertHistoricalData')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_InsertHistoricalData] (@HIST_Info nvarchar(200), @EmployeeID INT)
AS
BEGIN
DECLARE @CustomerID INT, @OutletID INT, @PackID INT, @Weight numeric(19,9), @CustomerCode nvarchar(20), 
@ItemCode nvarchar(20), @Price numeric(19,9), @Quantity numeric(19,9), @Value numeric(19,9),
@LastMonthQty numeric(19,9), @LastMonthValue numeric(19,9)

SET @CustomerCode = SUBSTRING(@HIST_Info,5,12) + 0
SET @ItemCode = SUBSTRING(@HIST_Info,19,18) + 0

SELECT @CustomerID=CustomerOutlet.CustomerID, @OutletID=CustomerOutlet.OutletID, @PackID=Pack.PackID, @Weight=Pack.Weight
FROM CustomerOutlet, Pack, Item
WHERE CustomerOutlet.CustomerCode = @CustomerCode AND Item.ItemID = Pack.ItemID AND Item.ItemCode = @ItemCode AND Pack.PackTypeID = 1

IF (@CustomerID IS NOT NULL AND @OutletID IS NOT NULL AND @PackID IS NOT NULL AND @Weight IS NOT NULL)
BEGIN
SET @Quantity = CAST(SUBSTRING(@HIST_Info,37,6) AS numeric(19,9)) / @Weight
SET @Value = CAST(SUBSTRING(@HIST_Info,49,8) AS numeric(19,9))
SET @Price = 0

IF (@Quantity <> 0)
BEGIN
SET @Price = @Value / @Quantity
END

IF NOT EXISTS (SELECT * FROM HistoryData WHERE Year = DATEPART(YYYY,GETDATE()) AND Month = DATEPART(MM,GETDATE()) 
AND EmployeeID = @EmployeeID AND CustomerID = @CustomerID AND OutletID = @OutletID AND PackID = @PackID)
BEGIN
INSERT INTO HistoryData VALUES (DATEPART(YYYY,GETDATE()),DATEPART(MM,GETDATE()),@HIST_Info,@EmployeeID,@CustomerID,@OutletID,@PackID,@Price,@Quantity)
END
ELSE BEGIN
UPDATE HistoryData SET Quantity = Quantity + @Quantity WHERE Year = DATEPART(YYYY,GETDATE()) 
AND Month = DATEPART(MM,GETDATE()) AND EmployeeID = @EmployeeID AND CustomerID = @CustomerID 
AND OutletID = @OutletID AND PackID = @PackID
END

SET @Quantity = CAST(SUBSTRING(@HIST_Info,43,6) AS numeric(19,9)) / @Weight
SET @Value = CAST(SUBSTRING(@HIST_Info,57,8) AS numeric(19,9))
SET @Price = 0

IF (@Quantity <> 0)
BEGIN
SET @Price = @Value / @Quantity
END

IF NOT EXISTS (SELECT * FROM HistoryData WHERE Year = DATEPART(YYYY,DATEADD(MM,-1,GETDATE())) 
AND Month = DATEPART(MM,DATEADD(MM,-1,GETDATE())) AND EmployeeID = @EmployeeID AND CustomerID = @CustomerID 
AND OutletID = @OutletID AND PackID = @PackID)
BEGIN
INSERT INTO HistoryData VALUES (DATEPART(YYYY,DATEADD(MM,-1,GETDATE())),DATEPART(MM,DATEADD(MM,-1,GETDATE())),@HIST_Info,@EmployeeID,@CustomerID,@OutletID,@PackID,@Price,@Quantity)
END
ELSE BEGIN
UPDATE HistoryData SET Quantity = Quantity + @Quantity WHERE Year = DATEPART(YYYY,DATEADD(MM,-1,GETDATE())) 
AND Month = DATEPART(MM,DATEADD(MM,-1,GETDATE())) AND EmployeeID = @EmployeeID AND CustomerID = @CustomerID 
AND OutletID = @OutletID AND PackID = @PackID
END

END
END
')

END

--sp_DeleteHistoricalData
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'sp_DeleteHistoricalData')
BEGIN

EXEC('
CREATE PROCEDURE [dbo].[sp_DeleteHistoricalData] (@EmployeeID INT)
AS
BEGIN

DELETE FROM TransactionDetail WHERE BatchNo IN (
SELECT CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + CAST(DATEPART(YY,GETDATE()) AS nvarchar(4))
UNION
SELECT CAST(DATEPART(MM,DATEADD(MM,-1,GETDATE())) AS nvarchar(2)) + CAST(DATEPART(YY,DATEADD(MM,-1,GETDATE())) AS nvarchar(4))
) AND SUBSTRING(TransactionID,1,CASE CHARINDEX(''-'',TransactionID) WHEN 0 THEN 1 ELSE CHARINDEX(''-'',TransactionID)-1 END) =cast( @EmployeeID as nvarchar(20))

DELETE FROM [Transaction] WHERE EmployeeID = @EmployeeID AND CreationReason IN (
SELECT CAST(CAST(DATEPART(MM,GETDATE()) AS nvarchar(2)) + CAST(DATEPART(YY,GETDATE()) AS nvarchar(4)) AS INT)
UNION
SELECT CAST(CAST(DATEPART(MM,DATEADD(MM,-1,GETDATE())) AS nvarchar(2)) + CAST(DATEPART(YY,DATEADD(MM,-1,GETDATE())) AS nvarchar(4)) AS INT))

DELETE FROM HistoryData WHERE EmployeeID = @EmployeeID 
AND ((Month = DATEPART(MM,GETDATE()) AND Year = DATEPART(YYYY,GETDATE())) OR (Month = DATEPART(MM,DATEADD(MM,-1,GETDATE())) AND Year = DATEPART(YYYY,DATEADD(MM,-1,GETDATE()))))
END
')

END