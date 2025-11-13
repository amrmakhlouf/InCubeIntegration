IF NOT EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'PriceDefinition_Temp') AND OBJECTPROPERTY(id, N'IsUserTable') = 1)
BEGIN

CREATE TABLE [dbo].[PriceDefinition_Temp](
	[PriceDefinitionID] [int] NOT NULL,
	[QuantityRangeID] [int] NOT NULL,
	[PacKID] [int] NOT NULL,
	[CurrencyID] [int] NOT NULL,
	[Tax] [numeric](19, 9) NOT NULL,
	[Price] [numeric](19, 9) NOT NULL,
	[PriceListID] [int] NOT NULL,
	[ExpiryDate] [datetime] NULL,
	[BatchNo] [nvarchar](100) NULL,
	[MinPrice] [numeric](19, 9) NOT NULL DEFAULT ((0)),
	[MaxPrice] [numeric](19, 9) NOT NULL DEFAULT ((0)),
)


PRINT 'Table PriceDefinition_Temp added successfully.'
END
ELSE
PRINT 'Table PriceDefinition_Temp already Exists.'