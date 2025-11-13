--Create table (PriceDetails)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'PriceDetails') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE PriceDetails(
      [EmployeeID] INT NOT NULL ,
      [TriggerID] INT NOT NULL ,
      [TableType] NVARCHAR(200) NOT NULL ,
      [PriceListCode] NVARCHAR(200) NOT NULL ,
      [GroupCode] NVARCHAR(200) NULL ,
      [CustomerCode] NVARCHAR(200) NULL ,
      [ItemCode] NVARCHAR(200) NOT NULL ,
      [SalesOrderType] NVARCHAR(200) NULL ,
      [Price] NUMERIC(19,9) NOT NULL ,
      [MinPrice] NUMERIC(19,9) NOT NULL ,
      [MaxPrice] NUMERIC(19,9) NOT NULL 
 );

PRINT 'Table PriceDetails added successfully.'
END
ELSE
PRINT 'Table PriceDetails already Exists.'

--Create table (DiscountDetails)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'DiscountDetails') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE DiscountDetails(
	  [ID] INT IDENTITY(1,1) NOT NULL ,
      [EmployeeID] INT NOT NULL ,
      [TriggerID] INT NOT NULL ,
      [TableType] NVARCHAR(200) NOT NULL ,
      [GroupCode] NVARCHAR(200) NULL ,
      [CustomerCode] NVARCHAR(200) NULL ,
      [ItemCode] NVARCHAR(200) NOT NULL ,
      [SalesOrderType] NVARCHAR(200) NULL ,
      [Discount] NUMERIC(19,9) NOT NULL 
 );

PRINT 'Table DiscountDetails added successfully.'
END
ELSE
PRINT 'Table DiscountDetails already Exists.'

--Create table (PriceHistory)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'PriceHistory') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE PriceHistory(
      [EmployeeID] INT NOT NULL ,
      [TriggerID] INT NOT NULL ,
      [PriceListID] BIGINT NULL ,
      [PriceListCode] NVARCHAR(200) NOT NULL ,
      [GroupID] INT NULL ,
      [CustomerID] INT NULL ,
      [OutletID] INT NULL ,
      [Priority] INT NOT NULL ,
      [SalesOrderTypeID] INT NOT NULL ,
      [PackID] INT NOT NULL ,
      [Price] NUMERIC(19,9) NOT NULL ,
      [MinPrice] NUMERIC(19,9) NULL ,
      [MaxPrice] NUMERIC(19,9) NULL ,
      [Discount] NUMERIC(19,9) NOT NULL ,

 CONSTRAINT [aaaaaPriceHistory_PK] PRIMARY KEY CLUSTERED 
(
  EmployeeID,TriggerID,PriceListCode,PackID
));

PRINT 'Table PriceHistory added successfully.'
END
ELSE
PRINT 'Table PriceHistory already Exists.'


IF NOT EXISTS (SELECT * FROM Configuration WHERE KeyName = 'ImportDiscount')
BEGIN

DECLARE @MAXConfigID INT;
SELECT @MAXConfigID = MAX(ConfigurationID)+1 FROM Configuration;
INSERT INTO Configuration values ('ImportDiscount','false',-1,@MAXConfigID);
INSERT INTO ConfigDefinition values (@MAXConfigID,2,0,1);
INSERT INTO ConfigDefinitionLanguage values (@MAXConfigID,1,'This configuration determines whether to have integration import discounts from SAP or not');
INSERT INTO ConfigurationOrganization VALUES (@MAXConfigID,7,'ImportDiscount','TRUE')

END

EXEC('
CREATE PROCEDURE sp_UpdatePrice 
(@EmployeeID INT,
@TriggerID INT,
@OrganizationID INT,
@DigitsCount INT,
@ProcessID INT)
AS
BEGIN

BEGIN TRY 

DECLARE @ResultID INT, @Message NVARCHAR(200), @Inserted INT

SET @ResultID = 2;
SET @Message = '''';
SET @Inserted = 0;

--Prepare Prices
INSERT INTO PriceHistory (EmployeeID,TriggerID,PriceListCode,GroupID,Priority,CustomerID,OutletID,SalesOrderTypeID
,PackID,Price,MinPrice,MaxPrice,Discount)
SELECT @EmployeeID EmployeeID, @TriggerID TriggerID, ISNULL(P.PriceListCode,D.PriceListCode) PriceListCode, P.GroupID
, ISNULL(P.Priority,1) Priority, ISNULL(P.CustomerID,D.CustomerID) CustomerID, ISNULL(P.OutletID,D.OutletID) OutletID
, ISNULL(P.SalesOrderTypeID,D.SalesOrderTypeID) SalesOrderTypeID, ISNULL(P.PackID,D.PackID) PackID
, ISNULL(P.Price,dbo.GetPrice(@EmployeeID,@TriggerID,D.CustomerCode,D.ItemCode,D.SalesOrderType)) Price
, P.MinPrice, P.MaxPrice, ISNULL(D.Discount,0) Discount
FROM
(SELECT PD.CustomerCode,CG.GroupID,CO.CustomerID,CO.OutletID,PD.ItemCode,IP.PackID,SalesOrderType
,ISNULL(SalesOrderTypeID,-1) SalesOrderTypeID,Price,MaxPrice,MinPrice,PriceListCode
,CASE PD.TableType WHEN ''01'' THEN 100 WHEN ''04'' THEN 50 ELSE 1 END Priority
FROM PriceDetails PD
LEFT JOIN CustomerGroup CG ON CG.GroupCode = PD.GroupCode
LEFT JOIN CustomerOutlet CO ON CO.CustomerCode = PD.CustomerCode
LEFT JOIN SalesOrderType SOT ON SOT.TypeCode = PD.SalesOrderType
INNER JOIN ItemDefaultPack IP ON IP.ItemCode = PD.ItemCode
WHERE PD.EmployeeID = @EmployeeID AND PD.TriggerID = @TriggerID AND (CG.GroupID IS NOT NULL OR CO.CustomerID IS NOT NULL)) P
FULL JOIN
(SELECT CustomerCode,CustomerID,OutletID,ItemCode,PackID,SalesOrderType
,ISNULL(SalesOrderTypeID,-1) SalesOrderTypeID,Discount,PriceListCode FROM (
SELECT RANK() OVER(PARTITION BY ISNULL(CO1.CustomerCode,CO2.CustomerCode), DD.ItemCode, SalesOrderType
ORDER BY (CASE TableType WHEN ''02'' THEN 1 WHEN ''03'' THEN 2 WHEN ''04'' THEN 3 WHEN ''01'' THEN 4 END) ASC, ID DESC) RNK,
ISNULL(CO1.CustomerCode,CO2.CustomerCode) CustomerCode, DD.ItemCode, SalesOrderType, Discount,
ISNULL(CO1.CustomerID,CO2.CustomerID) CustomerID, ISNULL(CO1.OutletID,CO2.OutletID) OutletID, IP.PackID, SOT.SalesOrderTypeID,
CASE DD.TableType WHEN ''05'' THEN (SELECT Email FROM Employee WHERE EmployeeID = DD.EmployeeID) +''-CustomerFEFO-'' + DD.SalesOrderType + ''-'' + + ISNULL(CO1.CustomerCode,CO2.CustomerCode)
ELSE (SELECT Email FROM Employee WHERE EmployeeID = DD.EmployeeID) +''-Customer-'' + ISNULL(CO1.CustomerCode,CO2.CustomerCode) END PriceListCode
FROM DiscountDetails DD
LEFT JOIN CustomerGroup CG ON CG.GroupCode = DD.GroupCode
LEFT JOIN CustomerOutletGroup COG ON COG.GroupID = CG.GroupID
LEFT JOIN CustomerOutlet CO1 ON CO1.CustomerID = COG.CustomerID AND CO1.OutletID = COG.OutletID
LEFT JOIN CustomerOutlet CO2 ON CO2.CustomerCode = DD.CustomerCode
INNER JOIN ItemDefaultPack IP ON IP.ItemCode = DD.ItemCode
LEFT JOIN SalesOrderType SOT ON SOT.TypeCode = DD.SalesOrderType
WHERE DD.EmployeeID = @EmployeeID AND DD.TriggerID = @TriggerID AND (CO1.CustomerID IS NOT NULL OR CO2.CustomerID IS NOT NULL)) t WHERE RNK = 1) D
ON P.PriceListCode = D.PriceListCode AND P.ItemCode = D.ItemCode

--Delete 0 price rows
DELETE FROM PriceHistory WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID AND Price = 0

--Fill Min, Max Prices
UPDATE PriceHistory SET MinPrice = Price WHERE MinPrice IS NULL
UPDATE PriceHistory SET MaxPrice = Price WHERE MaxPrice IS NULL

--Generate Price List IDs
DECLARE @SeedID INT
SELECT @SeedID=ISNULL(MAX(PriceListID),0) FROM PriceList WHERE PriceListID > @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000;
UPDATE PH SET PriceListID = PLID.PriceListID
FROM PriceHistory PH
INNER JOIN (SELECT PriceListCode, CAST(@EmployeeID AS NVARCHAR(3))
+ RIGHT(''000000'' + CAST(@SeedID+RANK() OVER(ORDER BY PriceListCode) AS NVARCHAR(10)),6) PriceListID FROM
(SELECT DISTINCT PriceListCode FROM PriceHistory WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID) t) PLID
ON PLID.PriceListCode = PH.PriceListCode
WHERE PH.EmployeeID = @EmployeeID AND PH.TriggerID = @TriggerID

--Create necessary groups
INSERT INTO CustomerGroup (GroupID,GroupCode)
SELECT GroupCode, RANK() OVER (ORDER BY GroupCode) + (SELECT MAX(GroupID) FROM CustomerGroup WHERE GroupID < 200000) FROM
(SELECT DISTINCT PD.GroupCode
FROM PriceDetails PD
LEFT JOIN CustomerGroup CG ON CG.GroupCode = PD.GroupCode
WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID AND TableType IN (''01'',''05'') AND CG.GroupID IS NULL) T

INSERT INTO CustomerGroupLanguage (GroupID,LanguageID,Description)
SELECT CG.GroupID,1,CG.GroupCode
FROM CustomerGroup CG
LEFT JOIN CustomerGroupLanguage CGL ON CGL.GroupID = CG.GroupID AND CGL.LanguageID = 1
WHERE CGL.Description IS NULL AND CG.GroupCode IS NOT NULL

BEGIN TRY 
BEGIN TRANSACTION T1

--Delete old prices
DELETE FROM PriceList WHERE PriceListID >  @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000
DELETE FROM PriceDefinition WHERE PriceListID >  @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000
DELETE FROM PriceListLanguage WHERE PriceListID >  @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000
DELETE FROM GroupPrice WHERE PriceListID >  @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000
DELETE FROM CustomerPrice WHERE PriceListID >  @EmployeeID * 1000000 AND PriceListID < (@EmployeeID+1) * 1000000

--Insert new pricelists
INSERT INTO PriceList 
(PriceListID,PriceListCode,StartDate,EndDate,Priority,PriceListTypeID,IsDeleted,OrganizationID,SalesOrderTypeID,StockStatusID)
SELECT DISTINCT PriceListID,PriceListCode,DATEADD(DD,-1,GETDATE()),DATEADD(YY,10,GETDATE()),Priority
,1,0,@OrganizationID,SalesOrderTypeID,-1
FROM PriceHistory WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID

INSERT INTO PriceListLanguage (PriceListID,LanguageID,Description)
SELECT PL.PriceListID,1,PL.PriceListCode
FROM PriceList PL
LEFT JOIN PriceListLanguage PLL ON PLL.PriceListID = PL.PriceListID AND PLL.LanguageID = 1
WHERE PLL.PriceListID IS NULL AND PL.PriceListCode IS NOT NULL

--Insert new price details
INSERT INTO PriceDefinition (PriceDefinitionID,PriceListID,PacKID,Price,MaxPrice,MinPrice,QuantityRangeID
,CurrencyID,Tax,ExpiryDate,BatchNo)
SELECT CAST(@EmployeeID AS NVARCHAR(4)) + RIGHT(''00000'' + CAST(RANK() OVER(ORDER BY PriceListID,PackID) AS NVARCHAR(5)), 5) PriceDefinitionID
, PriceListID, PackID
,ROUND(Price * (100 - (CASE WHEN Discount <= 100 THEN Discount ELSE 100 END)) / 100, @DigitsCount)
,ROUND(MaxPrice * (100 - (CASE WHEN Discount <= 100 THEN Discount ELSE 100 END)) / 100, @DigitsCount)
,ROUND(MinPrice * (100 - (CASE WHEN Discount <= 100 THEN Discount ELSE 100 END)) / 100, @DigitsCount)
, 1, 1, 0, ''1990/01/01'',''1990-01-01''
FROM PriceHistory WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID

SET @Inserted = @@ROWCOUNT;

--Link price lists
INSERT INTO CustomerPrice (PriceListID,CustomerID,OutletID)
SELECT DISTINCT PriceListID,CustomerID,OutletID
FROM PriceHistory
WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID AND CustomerID IS NOT NULL AND OutletID IS NOT NULL

INSERT INTO GroupPrice (PriceListID,GroupID)
SELECT DISTINCT PriceListID,GroupID
FROM PriceHistory
WHERE EmployeeID = @EmployeeID AND TriggerID = @TriggerID AND GroupID IS NOT NULL AND CustomerID IS NULL

COMMIT TRANSACTION T1
SET @ResultID = 1;
SET @Message = ''Success'';
END TRY
BEGIN CATCH
ROLLBACK TRANSACTION T1
SET @Message = ERROR_MESSAGE();
END CATCH

END TRY
BEGIN CATCH
SET @Message = ERROR_MESSAGE();
END CATCH

UPDATE Int_ExecutionDetails SET Inserted = @Inserted, RunTimeEnd = GETDATE(), ResultID = @ResultID, [Message] = @Message WHERE ID = @ProcessID;

END
')

EXEC('
CREATE FUNCTION dbo.GetPrice 
(@EmployeeID INT, @TriggerID INT, @CustomerCode NVARCHAR(20), @ItemCode NVARCHAR(20), @OrderType NVARCHAR(20))
RETURNS DECIMAL(19,9)
AS
BEGIN
DECLARE @Price DECIMAL(19,9);
SET @Price = 0;

SELECT TOP 1 @Price = Price
FROM PriceDetails PD
LEFT JOIN CustomerGroup CG ON CG.GroupCode = PD.GroupCode
LEFT JOIN CustomerOutletGroup COG ON COG.GroupID = CG.GroupID
LEFT JOIN CustomerOutlet CO1 ON CO1.CustomerID = COG.CustomerID AND CO1.OutletID = COG.OutletID
LEFT JOIN CustomerOutlet CO2 ON CO2.CustomerID = PD.CustomerCode
WHERE PD.EmployeeID = @EmployeeID AND PD.TriggerID = @TriggerID
AND ISNULL(CO1.CustomerCode,CO2.CustomerCode) = @CustomerCode AND PD.ItemCode = @ItemCode AND (SalesOrderType = @OrderType OR SalesOrderType = '''')
ORDER BY CASE SalesOrderType WHEN @OrderType THEN 0 ELSE 1 END 
,CASE PD.TableType WHEN ''08'' THEN 1 WHEN ''05'' THEN 2 WHEN ''02'' THEN 3 WHEN ''03'' THEN 4 WHEN ''04'' THEN 5 WHEN ''01'' THEN 6 ELSE 10 END;

RETURN @Price;

END;
')

EXEC ('
CREATE VIEW ItemDefaultPack AS
SELECT ItemCode,PackID FROM
(SELECT I.ItemCode,I.ItemID,P.PackID,P.PackTypeID,PTL.Description ''UOM''
,RANK() OVER (PARTITION BY I.ItemCode ORDER BY CASE PTL.Description WHEN ''CX'' THEN 0 ELSE 1 END, PTL.Description) RNK
FROM Item I
INNER JOIN Pack P ON P.ItemID = I.ItemID
INNER JOIN PackTypeLanguage PTL ON PTL.PackTypeID = P.PackTypeID AND PTL.LanguageID = 1) T WHERE RNK = 1
')