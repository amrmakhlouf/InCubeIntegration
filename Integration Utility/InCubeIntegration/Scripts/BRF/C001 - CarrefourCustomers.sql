--Create table (CarrefourCustomers)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'CarrefourCustomers') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE CarrefourCustomers(
      [CarrefourCode] NVARCHAR(50) NOT NULL ,
      [BRFCode] NVARCHAR(50) NOT NULL ,
      [CustomerID] INT NOT NULL ,
      [OutletID] INT NOT NULL 
 );

PRINT 'Table CarrefourCustomers added successfully.'
END
ELSE
PRINT 'Table CarrefourCustomers already Exists.'


--Create table (CarrefourItems)
IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'CarrefourItems') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN
CREATE TABLE CarrefourItems(
      [Barcode] NVARCHAR(50) NOT NULL ,
      [BRFCode] NVARCHAR(50) NOT NULL ,
      [PackID] INT NOT NULL ,
      [Factor] NUMERIC(19,9) NOT NULL 
 );

PRINT 'Table CarrefourItems added successfully.'
END
ELSE
PRINT 'Table CarrefourItems already Exists.'