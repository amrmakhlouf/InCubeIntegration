--Update Items
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateItems]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN
BEGIN try
BEGIN TRANSACTION T1
---------ITEMS----------------------------------------------------
DECLARE @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);
update e set e.DIVISIONID=1, e.UPDATEDDATE=GETDATE() from ItemCategory e 
inner join Cubita_test.dbo.Incube_item_master v on e.ItemCATEGORYCODE collate SQL_Latin1_General_CP1256_CI_AS=v.family

delete from ITEMCATEGORYLANGUAGE where ITEMCATEGORYID in (select ITEMCATEGORYID from ITEMCATEGORY where ITEMCATEGORYCODE collate SQL_Latin1_General_CP1256_CI_AS in (select FAMILY from Cubita_test.dbo.Incube_item_master))
insert into ItemCategoryLanguage
select e.ITEMCATEGORYID,1,v.FAMILY from ItemCategory e inner join Cubita_test.dbo.Incube_item_master v on e.ITEMCATEGORYCODE collate SQL_Latin1_General_CP1256_CI_AS=v.FAMILY
group by e.ITEMCATEGORYID,v.FAMILY
declare @MaxCATID int=isnull((select max(isnull(ITEMCATEGORYID,0))+1 from ItemCategory),1);
insert into ItemCategory
OUTPUT inserted.ITEMCATEGORYID,inserted.ITEMCATEGORYCODE into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by FAMILY)+@MaxCATID AS ITEMCATEGORYID,v.FAMILY AS ITEMCATEGORYCODE,1,0,getdate(),null,null
from Cubita_test.dbo.Incube_item_master v 
where not exists(select ITEMCATEGORYCODE from ItemCategory where ITEMCATEGORYCODE collate SQL_Latin1_General_CP1256_CI_AS=v.FAMILY)
GROUP BY v.FAMILY

update t set t.description=v.FAMILY from @TempEmp t inner join Cubita_test.dbo.Incube_item_master v on t.CODE collate SQL_Latin1_General_CP1256_CI_AS=v.FAMILY

insert into ITEMCATEGORYLANGUAGE
select ID,1,description from @TempEmp

delete from @TempEmp
------------------------------------
declare @MaxBrandID int=isnull((select max(isnull(BrandID,0))+1 from Brand),1);
insert into Brand
select ROW_NUMBER()over(order by brand)+@MaxBrandID AS BrandID
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from brandlanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.brand)
GROUP BY v.brand

insert into brandlanguage
select ROW_NUMBER()over(order by brand)+@MaxBrandID AS BrandID,1,v.brand AS ITEMCATEGORYCODE
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from brandlanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.brand)
GROUP BY v.brand

delete from @TempEmp

------------------------------------

declare @MaxGroupID int=isnull((select max(isnull(PackGroupID,0))+1 from PackGroup),1);
insert into PackGroup
OUTPUT inserted.PackGroupID,inserted.PackGroupCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by SubFamily)+@MaxGroupID AS ITEMCATEGORYID,v.SubFamily AS ITEMCATEGORYCODE,null,0,0
from Cubita_test.dbo.Incube_item_master v 
where not exists(select PackGroupCode from PackGroup where PackGroupCode collate SQL_Latin1_General_CP1256_CI_AS=v.SubFamily)
GROUP BY v.SubFamily

insert into PackGroupLANGUAGE
select ID,1,CODE from @TempEmp

delete from @TempEmp
-------------------------------------
declare @MaxPTID int=isnull((select max(isnull(PackTypeID,0))+1 from PackType),1);
insert into PackType
select ROW_NUMBER()over(order by baseUnit)+@MaxPTID AS PackTypeID
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from PackTypeLanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.BaseUnit)
GROUP BY v.baseUnit

insert into PackTypeLanguage
select ROW_NUMBER()over(order by baseUnit)+@MaxPTID AS PackTypeID,1,v.BaseUnit AS ITEMCATEGORYCODE
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from PackTypeLanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.BaseUnit)
GROUP BY v.baseUnit

delete from @TempEmp


set @MaxPTID =isnull((select max(isnull(PackTypeID,0))+1 from PackType),1);
insert into PackType
select ROW_NUMBER()over(order by SmallUnit)+@MaxPTID AS PackTypeID
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from PackTypeLanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.SmallUnit)
GROUP BY v.SmallUnit

insert into PackTypeLanguage
select ROW_NUMBER()over(order by SmallUnit)+@MaxPTID AS PackTypeID,1,v.SmallUnit AS ITEMCATEGORYCODE
from Cubita_test.dbo.Incube_item_master v 
where not exists(select description from PackTypeLanguage where description collate SQL_Latin1_General_CP1256_CI_AS=v.SmallUnit)
GROUP BY v.SmallUnit

delete from @TempEmp
-------------------------------------
update e set e.ItemCategoryID=ic.itemcategoryid, e.inactive=v.ia_item_blocked,e.brandid=bl.brandid 
from item e 
inner join Cubita_test.dbo.Incube_item_master v on e.itemcode collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code
inner join itemcategory ic on v.family=ic.ItemCategorycode collate SQL_Latin1_General_CP1256_CI_AS
inner join brandlanguage bl on bl.description collate SQL_Latin1_General_CP1256_CI_AS=v.brand and bl.languageid=1

delete from ItemLanguage where ItemID in (select ItemID from Item where ItemCode collate SQL_Latin1_General_CP1256_CI_AS in (select ia_item_code from Cubita_test.dbo.Incube_item_master))
insert into ItemLanguage
select e.Itemid,1,v.ia_NAME from Item e inner join Cubita_test.dbo.Incube_item_master v on e.ItemCode collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code

declare @MaxItemID int=isnull((select max(isnull(ItemID,0))+1 from Item),1);
insert into Item (ItemID,ItemCategoryID,Inactive,ItemCode,PackDefinition,Origin,CreatedBy,CreatedDate,ItemType,ForceDefaultPack,BrandID,IsBatchPriced)
OUTPUT inserted.ItemID,inserted.ItemCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by ia_item_code)+@MaxItemID AS ItemID,ic.itemcategoryid,v.ia_item_blocked,v.ia_item_code AS ItemCode,
v.[types],''Cubita'',0,getdate(),1,0,bl.brandid,0
from Cubita_test.dbo.Incube_item_master v 
inner join itemcategory ic on v.family=ic.ItemCategorycode collate SQL_Latin1_General_CP1256_CI_AS
inner join brandlanguage bl on bl.description collate SQL_Latin1_General_CP1256_CI_AS=v.brand and bl.languageid=1
where not exists(select ItemCode from Item where ItemCode collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code)


update t set t.description=v.ia_NAME from @TempEmp t inner join Cubita_test.dbo.Incube_item_master v on t.CODE collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code

insert into ItemLanguage
select ID,1,description from @TempEmp

-----------------------------------------
declare @MaxPackID int=isnull((select max(isnull(PackID,0))+1 from Pack),1);
insert into Pack (PackID,Barcode,ItemID,PackTypeID,Quantity,EquivalencyFactor,HasSerialNumber)

select ROW_NUMBER()over(order by ia_item_code,baseUnit)+@MaxPackID AS PackID,v.ia_item_code AS Barcode,
i.itemid,ptl.packtypeid,v.Factor,1,0
from Cubita_test.dbo.Incube_item_master v 
inner join Item i on v.ia_item_code=i.ItemCode collate SQL_Latin1_General_CP1256_CI_AS
inner join PackTypeLanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=v.baseUnit and ptl.languageid=1
where not exists(select Barcode,PackTypeID,Quantity from Pack where Barcode collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code and PacktypeID=ptl.packtypeid and Quantity=v.Factor)


set @MaxPackID =isnull((select max(isnull(PackID,0))+1 from Pack),1);
insert into Pack (PackID,Barcode,ItemID,PackTypeID,Quantity,EquivalencyFactor,HasSerialNumber)

select ROW_NUMBER()over(order by ia_item_code,SmallUnit)+@MaxPackID AS PackID,v.ia_item_code AS Barcode,
i.itemid,ptl.packtypeid,1,1,0
from Cubita_test.dbo.Incube_item_master v 
inner join Item i on v.ia_item_code=i.ItemCode collate SQL_Latin1_General_CP1256_CI_AS
inner join PackTypeLanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=v.SmallUnit and ptl.languageid=1
where not exists(select Barcode,PackTypeID,Quantity from Pack where Barcode collate SQL_Latin1_General_CP1256_CI_AS=v.ia_item_code and PacktypeID=ptl.packtypeid and Quantity=1)


---------END OF ITEMS---------------------------------------------
COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdateItems'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Employees
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateEmployee]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN

---------EMPLOYEE------------------------------------------
BEGIN try
BEGIN TRANSACTION T1
update e set e.phone=v.phone, e.inactive=v.status from employee e inner join Cubita_test.dbo.InCube_Salesman_master v on e.employeecode collate SQL_Latin1_General_CP1256_CI_AS=v.salesman_code where e.employeeid<>0
delete from employeelanguage where employeeid<>0 and employeeid in (select employeeid from employee where employeecode in (select salesman_code from Cubita_test.dbo.InCube_Salesman_master))
insert into employeelanguage
select e.employeeid,1,v.salesman_name,e.employeecode from employee e inner join Cubita_test.dbo.InCube_Salesman_master v on e.employeecode=v.salesman_code
where e.employeeid<>0
DECLARE @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);
declare @MaxEmpID int=isnull((select max(isnull(employeeid,0))+1 from employee),1);
insert into Employee
OUTPUT inserted.EmployeeID,inserted.EmployeeCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by Salesman_Code)+@MaxEmpID AS EmployeeID,v.Salesman_Code AS EmployeeCode,v.Phone,v.Phone,null,null,1,v.Status,2,0,0,getdate(),null,null,
null,null,null,null,null,null from Cubita_test.dbo.InCube_Salesman_master v 
where not exists(select employeecode from employee where employeecode=v.Salesman_Code)

update t set t.description=v.Salesman_Name from @TempEmp t inner join Cubita_test.dbo.InCube_Salesman_master v on t.CODE=v.salesman_code

insert into employeelanguage
select ID,1,description,CODE from @TempEmp

delete from @TempEmp
COMMIT TRANSACTION T1
---------END OF EMPLOYEE------------------------------------------
end try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdateEmployee'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Customers
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateCustomer]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN
BEGIN try
BEGIN TRANSACTION T1
---------CUSTOMER-------------------------------------------------
DECLARE @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);
declare @MAXPaymentTermID int=isnull((select max(isnull(PaymentTermID,0))+1 from PaymentTerm),1);
insert into PaymentTerm
OUTPUT inserted.PaymentTermID,cast(inserted.SimplePeriodWidth as nvarchar) into @TempEmp(ID,CODE)

select ROW_NUMBER()over(order by v.CreditDays)+@MAXPaymentTermID PaymentTermID,1,v.CreditDays PaymentTermDays,1,null,null,null,null
from Cubita_test.dbo.InCube_Customer_master v 
where not exists(select SimplePeriodWidth from PaymentTerm where SimplePeriodWidth=v.CreditDays)
group by v.CreditDays

update t set t.description=''Every''+cast(v.CreditDays as nvarchar)+''Days'' from @TempEmp t inner join Cubita_test.dbo.InCube_Customer_master v on cast(t.code as int)=v.CreditDays

insert into PaymentTermLanguage
select ID,1,description from @TempEmp
delete from @TempEmp
----------------------------

update a set a.creditlimit=v.creditlimit from account a inner join accountcust ac on a.accountid=ac.AccountID
inner join customer c on c.customerid=ac.customerid inner join Cubita_test.dbo.InCube_Customer_master v on c.customercode=v.customer_code

update e set  e.onhold=v.status from Customer e inner join Cubita_test.dbo.InCube_Customer_master v on e.Customercode=v.customer_code

delete from Customerlanguage where Customerid in (select Customerid from Customer where Customercode in (select customer_code from Cubita_test.dbo.InCube_Customer_master))

insert into Customerlanguage (CustomerID,LanguageID,Description)
select e.Customerid,1,v.customer_name from Customer e inner join Cubita_test.dbo.InCube_Customer_master v on e.Customercode=v.customer_code

declare @maxAccCustID int=isnull((select max(isnull(AccountID,0))+1 from Account),1);
declare @MaxCustomerID int=isnull((select max(isnull(Customerid,0))+1 from Customer),1);
insert into Customer (CustomerID,CustomerCode,OnHold,StreetID,Inactive,New,CreatedBy,CreatedDate,Downloaded)
OUTPUT inserted.CustomerID,inserted.CustomerCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by customer_Code)+@MaxCustomerID AS CustomerID,v.customer_Code AS CustomerCode,v.Status,0,0,0,0,getdate(),1 
from Cubita_test.dbo.InCube_Customer_master v 
where not exists(select Customercode from Customer where Customercode=v.customer_Code)

update t set t.description=v.customer_Name from @TempEmp t inner join Cubita_test.dbo.InCube_Customer_master v on t.CODE=v.customer_code

insert into Customerlanguage
select ID,1,description,CODE from @TempEmp

insert into Account (AccountID,AccountTypeID,CreditLimit,Balance,GL,OrganizationID,CurrencyID,ParentAccountID)
select ID+@maxAccCustID,1,v.creditlimit,0,0,1,1,null from @TempEmp t inner join Cubita_test.dbo.InCube_Customer_master v on t.CODE=v.customer_code 

insert into AccountLanguage
select ID+@maxAccCustID,1,CODE from @TempEmp

insert into AccountCust
select ID,ID+@maxAccCustID from @TempEmp

--delete from @TempEmp
-----------------------------


declare @maxAccCustOutID int=isnull((select max(isnull(AccountID,0))+1 from Account),1);
declare @MaxCustomerOutletID int=isnull((select max(isnull(Customerid,0))+1 from CustomerOutlet),1);

insert into CustomerOutlet(CustomerID,OutletID,CustomerCode,Phone,Fax,Email,Barcode,OnHold,GPSLatitude,GPSLongitude,StreetID,Inactive
,PaymentTermID,CustomerTypeID,CreatedBy,CreatedDate,CustomerClassID,OrganizationID)

select t.ID AS CustomerID,1,t.CODE AS CustomerCode,null,null,
null,t.CODE AS CustomerCode,v.Status,0,0,0,0,
PT.PaymentTermID,case(v.CreditDays) when 0 then 1 else 2 end,0,getdate(),null,1 
from Cubita_test.dbo.InCube_Customer_master v 
inner join paymentterm pt on v.creditdays=pt.SimplePeriodWidth
inner join @TempEmp t on t.CODE=v.customer_code
where not exists(select Customercode from CustomerOutlet where Customercode=v.customer_Code)

insert into CustomerOutletlanguage (CustomerID,OutletID,LanguageID,Description)
select ID,1,1,description from @TempEmp


update a set a.creditlimit=v.creditlimit from account a inner join accountcustOut ac on a.accountid=ac.AccountID
inner join customerOutlet c on c.customerid=ac.customerid and c.outletid=ac.outletid inner join Cubita_test.dbo.InCube_Customer_master v on c.customercode=v.customer_code

update e set  e.onhold=v.status from CustomerOutlet e inner join Cubita_test.dbo.InCube_Customer_master v on e.Customercode=v.customer_code
delete from CustomerOutletlanguage where Customerid in (select Customerid from Customer where Customercode in (select customer_code from Cubita_test.dbo.InCube_Customer_master))
insert into CustomerOutletlanguage (CustomerID,OutletID,LanguageID,Description)
select e.Customerid,1,1,v.customer_name from Customer e inner join Cubita_test.dbo.InCube_Customer_master v on e.Customercode=v.customer_code

insert into Account (AccountID,AccountTypeID,CreditLimit,Balance,GL,OrganizationID,CurrencyID,ParentAccountID)
select ID+@maxAccCustOutID,1,v.creditlimit,0,0,1,1,ac.accountid from @TempEmp t 
inner join Cubita_test.dbo.InCube_Customer_master v on t.CODE=v.customer_code 
inner join accountcust ac on ac.customerid=t.ID

insert into AccountLanguage
select ID+@maxAccCustOutID,1,CODE+''/1'' from @TempEmp

insert into AccountCustOut
select ID,1,ID+@maxAccCustOutID from @TempEmp

delete from @TempEmp
------------------------------
delete from CustOutTerritory
delete from routecustomer

insert into CustOutTerritory
select co.customerid,co.outletid,t.territoryid
from Cubita_test.dbo.InCube_Customer_master v 
inner join customeroutlet co on v.customer_code=co.customercode
inner join Cubita_test.dbo.InCube_Salesman_master sm on v.Salesman_name=sm.Salesman_Name
inner join territory t on t.territorycode=sm.salesman_code

insert into routecustomer (RouteID,CustomerID,OutletID)
select t.RouteID,co.customerid,co.outletid
from Cubita_test.dbo.InCube_Customer_master v 
inner join customeroutlet co on v.customer_code=co.customercode
inner join Cubita_test.dbo.InCube_Salesman_master sm on v.Salesman_name=sm.Salesman_Name
inner join Route t on t.RouteCode=sm.salesman_code

DELETE FROM @TempEmp
---------END OF CUSTOMER------------------------------------------

COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdateCustomer'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Routes
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateRoute]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN
---------ROUTES AND TERRITORIES-----------------------------------
declare  @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);

BEGIN try
BEGIN TRANSACTION T1
delete from TERRITORYLANGUAGE where TERRITORYID in (select TERRITORYID from TERRITORY where TERRITORYCODE in (select salesman_code from Cubita_test.dbo.InCube_Salesman_master))
insert into TERRITORYLANGUAGE
select e.TERRITORYID,1,v.salesman_name from TERRITORY e inner join Cubita_test.dbo.InCube_Salesman_master v on e.TERRITORYCODE=v.salesman_code


declare @MAXTERRITORYID int=isnull((select max(isnull(TERRITORYID,0))+1 from TERRITORY),1);
insert into TERRITORY
OUTPUT inserted.territoryid,inserted.territorycode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by Salesman_Code)+@MAXTERRITORYID EmployeeID,1,0,GETDATE(),NULL,NULL,v.Salesman_Code EmployeeCode 
from Cubita_test.dbo.InCube_Salesman_master v 
where not exists(select TERRITORYCODE from TERRITORY where TERRITORYCODE=v.Salesman_Code)

update t set t.description=v.Salesman_Name from @TempEmp t inner join Cubita_test.dbo.InCube_Salesman_master v on t.code=v.salesman_code

insert into TERRITORYLANGUAGE
select ID,1,description from @TempEmp

-------------------------------------------------------------------


delete from ROUTELANGUAGE where ROUTEID in (select ROUTEID from ROUTE where ROUTECODE in (select salesman_code from Cubita_test.dbo.InCube_Salesman_master))
insert into ROUTELANGUAGE (RouteID,LanguageID,Description)
select e.ROUTEID,1,v.salesman_name from ROUTE e inner join Cubita_test.dbo.InCube_Salesman_master v on e.ROUTECODE=v.salesman_code


declare @MAXROUTEID int=isnull((select max(isnull(ROUTEID,0))+1 from ROUTE),1);
insert into ROUTE (RouteID,Inactive,TerritoryID,CreatedBy,CreatedDate,CustomerID,OutletID,RouteCode)
OUTPUT inserted.Routeid,inserted.RouteCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by Salesman_Code)+@MAXROUTEID EmployeeID,0,T.TERRITORYID,0,GETDATE(),-1,-1,v.Salesman_Code EmployeeCode
from Cubita_test.dbo.InCube_Salesman_master v 
inner join territory T on t.territorycode=v.Salesman_Code
where not exists(select ROUTECODE from ROUTE where ROUTECODE=v.Salesman_Code)

update t set t.description=v.Salesman_Name from @TempEmp t inner join Cubita_test.dbo.InCube_Salesman_master v on t.CODE=v.salesman_code

insert into ROUTELANGUAGE
select ID,1,description from @TempEmp

insert into RouteVisitPattern
select ID,1,1,1,1,1,1,1,1 from @TempEmp

delete from @TempEmp
---------END OF ROUTES AND TERRITORIES----------------------------
COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdateRoutes'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Warehouses
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateWarehouse]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN
---------WAREHOUSES-----------------------------------
BEGIN try
BEGIN TRANSACTION T1
delete from WarehouseLanguage where WarehouseID in (select WarehouseID from Warehouse where WarehouseCODE collate SQL_Latin1_General_CP1256_CI_AS in (select Warehouse_Code from Cubita_test.dbo.InCube_Warehouses))
insert into WarehouseLanguage
select e.WarehouseID,1,v.Warehouse_Description,null from Warehouse e inner join Cubita_test.dbo.InCube_Warehouses v on e.WarehouseCODE collate SQL_Latin1_General_CP1256_CI_AS=v.Warehouse_Code
DECLARE @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);

declare @MAXWarehouseID int=isnull((select max(isnull(WarehouseID,0))+1 from Warehouse),1);
insert into Warehouse
OUTPUT inserted.warehouseID,inserted.warehouseCode into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by Warehouse_Code)+@MAXWarehouseID warehouseID,null,null,v.Warehouse_Code,1,0,GETDATE(),NULL,NULL,2,v.Warehouse_Code warehouseCode,0 
from Cubita_test.dbo.InCube_Warehouses v 
where not exists(select WarehouseCODE from Warehouse where WarehouseCODE collate SQL_Latin1_General_CP1256_CI_AS=v.Warehouse_Code)

update t set t.description =v.Warehouse_Description  collate SQL_Latin1_General_CP1256_CI_AS from @TempEmp t inner join Cubita_test.dbo.InCube_Warehouses v on t.code collate SQL_Latin1_General_CP1256_CI_AS=v.Warehouse_Code

insert into WarehouseLanguage
select ID,1,description,null from @TempEmp

insert into warehousezone
select ID,1 from @TempEmp

insert into warehousezonelanguage
select ID,1,1,description from @TempEmp

insert into vehicle
select ID,''NA'',1,null,null,null,null,0,getdate(),null,null,null,null from @TempEmp

delete from @TempEmp
---------END OF WAREHOUSES----------------------------------------

COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdateWarehouse'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Prices
EXEC('
CREATE  PROCEDURE [dbo].[sp_UpdatePrice]
(
@TriggerID INT,
@UserID INT
)
AS
BEGIN
BEGIN try
BEGIN TRANSACTION T1
---------DEFAULT PRICES---------------------------------------------------

--Declare @MaxPLID int=isnull((select max(isnull(PriceListID,0))+1 from PriceList),1);
insert into PriceList
select 1,''Default'',''1990-01-01'',''2020-01-01'',1,1,0,1,null,-1
where not exists(select pricelistcode from pricelist where  pricelistcode=''Default'')
DECLARE @TempEmp TABLE  
(  
    ID INT,  
    CODE VARCHAR(32),
	Description VARCHAR(100)  
);
insert into pricelistlanguage
select 1,1,''Default''  where not exists(select description from pricelistlanguage where  description=''Default'')

begin transaction

delete from pricedefinition

insert into pricedefinition
select ROW_NUMBER()over(order by ia_item_code,Unit),1,p.packid,1,0,isnull(v.price,0),1,null,null,-1,-1
from Cubita_test.dbo.Incube_Item_price v
inner join item i on v.ia_item_code=i.itemcode collate SQL_Latin1_General_CP1256_CI_AS
inner join packtypelanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=v.Unit and ptl.languageid=1
inner join pack p on i.itemid=p.itemid and p.quantity=1 and ptl.PackTypeID=p.PackTypeID
where v.price is not null and v.price>0
commit transaction

---------CUSTOMER PRICES---------------------------------------------------

DELETE FROM PRICELISTLANGUAGE WHERE PRICELISTID IN (SELECT PRICELISTID FROM PRICELIST WHERE PRICELISTCODE<>''DEFAULT'')
DELETE FROM PRICELIST WHERE PRICELISTCODE<>''DEFAULT''
DELETE FROM CUSTOMERPRICE

declare @MaxPLID int=isnull((select max(isnull(PRICELISTID,0))+1 from PRICELIST),1);
insert into PRICELIST
OUTPUT inserted.PRICELISTID,inserted.PRICELISTCODE into @TempEmp(ID,CODE)
select ROW_NUMBER()over(order by PR_PC_CODE)+@MaxPLID AS PRICELISTID,v.PR_PC_CODE AS PRICELISTCODE,
''2016-01-01'',''2020-01-01'',1,1,0,1,NULL,-1
from Cubita_test.dbo.Incube_Customer_Item_price v 
where not exists(select PRICELISTCODE from PRICELIST where PRICELISTCODE collate SQL_Latin1_General_CP1256_CI_AS=v.PR_PC_CODE)


update t set t.description=v.PR_PC_CODE from @TempEmp t inner join Cubita_test.dbo.Incube_Customer_Item_price v on t.CODE collate SQL_Latin1_General_CP1256_CI_AS=v.PR_PC_CODE

insert into PRICELISTLANGUAGE
select ID,1,description from @TempEmp

Declare @MaxPDID int=isnull((select max(isnull(Pricedefinitionid,0))+1 from pricedefinition),1);
begin transaction

insert into pricedefinition
select ROW_NUMBER()over(order by ia_item_code,Unit)+@MaxPDID,1,p.packid,1,0,isnull(v.price,0) price,1,null,null,-1,-1
from Cubita_test.dbo.Incube_Customer_Item_price v
inner join item i on v.ia_item_code=i.itemcode collate SQL_Latin1_General_CP1256_CI_AS
inner join packtypelanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=v.Unit and ptl.languageid=1
inner join pack p on i.itemid=p.itemid and ptl.PackTypeID=p.PackTypeID
where v.price is not null and isnull(v.price,0)>0  AND V.ISDEFAULT=0

commit transaction

INSERT INTO CUSTOMERPRICE
SELECT CO.CUSTOMERID,CO.OUTLETID,PL.PRICELISTID
FROM Cubita_test.dbo.Incube_Customer_Item_price V
INNER JOIN CUSTOMEROUTLET CO ON V.CustomerId=CO.CUSTOMERCODE
INNER JOIN PRICELIST PL ON PL.PRICELISTCODE=V.pr_pc_code collate SQL_Latin1_General_CP1256_CI_AS
where v.price is not null and isnull(v.price,0)>0  AND V.ISDEFAULT=0

---------END OF PRICES---------------------------------------------
COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
INSERT INTO ERROR_TRACK (ErrorNumber,ProcedureName,ErrorLine,ErrorMessage,ErrorDate)
SELECT ERROR_NUMBER(), ''sp_UpdatePrice'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
GO
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
------------------------------------------------------------------------------------
--Update Stock
EXEC('
CREATE PROCEDURE [dbo].[sp_UpdateStock]
(
@TriggerID INT
)
AS
BEGIN
BEGIN try
BEGIN TRANSACTION T1
---------STOCK-----------------------------------------------------
DECLARE @VEHICLECODE NVARCHAR(50),@VEHICLEID INT,@UPLOADED BIT;
DECLARE VLOOP CURSOR FOR
SELECT WAREHOUSEID,WAREHOUSECODE FROM WAREHOUSE WHERE WAREHOUSETYPEID=2 and WarehouseCode collate SQL_Latin1_General_CP1256_CI_AS in (select Warehouse_Code from Cubita_test.dbo.InCube_Warehouses_stocks)
OPEN VLOOP
FETCH NEXT FROM VLOOP INTO @VEHICLEID,@VEHICLECODE
WHILE (@@FETCH_STATUS=0)
BEGIN
SET @UPLOADED=ISNULL((SELECT TOP(1) UPLOADED FROM ROUTEHISTORY WHERE VEHICLEID=@VEHICLEID ORDER BY ROUTEHISTORYID DESC),0)
IF(@UPLOADED=0)
BEGIN
DELETE FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@VEHICLEID
DECLARE @WAREHOUSEID INT, @PACKID INT,@QUANTITY DECIMAL(18,2),@ITEMID INT,@BATCHNO NVARCHAR(50),@EXPDATE DATETIME
DECLARE PLOOP CURSOR FOR
SELECT W.WAREHOUSEID,P.PACKID,CS.qty,I.ITEMID,''1990-01-01'' INVENTBATCHID,''1990-01-01 00:00:00.000''EXPDATE
FROM Cubita_test.dbo.InCube_Warehouses_stocks CS
INNER JOIN WAREHOUSE W ON CS.WAREHOUSE_CODE=W.WAREHOUSECODE collate SQL_Latin1_General_CP1256_CI_AS
INNER JOIN ITEM I ON I.ITEMCODE collate SQL_Latin1_General_CP1256_CI_AS=CS.ITEMCODE --and i.Origin=cs.flag
inner join packtypelanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=cs.uom
inner join pack p on p.itemid=i.itemid and p.PackTypeID=ptl.PackTypeID
WHERE W.WAREHOUSEID=@VEHICLEID and I.Inactive=0
OPEN PLOOP
FETCH NEXT FROM PLOOP INTO @WAREHOUSEID , @PACKID ,@QUANTITY,@ITEMID,@BATCHNO,@EXPDATE
WHILE(@@FETCH_STATUS=0)
BEGIN

----------------------------------------
DECLARE @PACKID2 INT;
DECLARE LOOP2 CURSOR FOR
SELECT PACKID FROM PACK WHERE ITEMID=@ITEMID AND PACKID<>@PACKID
OPEN LOOP2
FETCH NEXT FROM LOOP2 INTO @PACKID2
WHILE (@@FETCH_STATUS=0)
BEGIN
IF EXISTS(SELECT * FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID2 AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE)
BEGIN
UPDATE WAREHOUSESTOCK SET QUANTITY=QUANTITY+0, BASEQUANTITY= BASEQUANTITY+0 WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID2 AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE
END
ELSE
BEGIN
INSERT INTO WAREHOUSESTOCK
SELECT @WAREHOUSEID,1,@PACKID2,@EXPDATE,@BATCHNO,0,NULL,0,0,-1
END

FETCH NEXT FROM LOOP2 INTO @PACKID2
END
CLOSE LOOP2
DEALLOCATE LOOP2

IF EXISTS(SELECT * FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE)
BEGIN
UPDATE WAREHOUSESTOCK SET QUANTITY=QUANTITY+@QUANTITY, BASEQUANTITY= BASEQUANTITY+@QUANTITY WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE
END
ELSE
BEGIN
INSERT INTO WAREHOUSESTOCK
SELECT @WAREHOUSEID,1,@PACKID,@EXPDATE,@BATCHNO,@QUANTITY,NULL,0,@QUANTITY,-1
END

----------------------------------------

FETCH NEXT FROM PLOOP INTO @WAREHOUSEID , @PACKID ,@QUANTITY,@ITEMID,@BATCHNO,@EXPDATE
END
CLOSE PLOOP
DEALLOCATE PLOOP
END

FETCH NEXT FROM VLOOP INTO @VEHICLEID,@VEHICLECODE
END
CLOSE VLOOP
DEALLOCATE VLOOP
--UPDATE WAREHOUSESTOCK SET BATCHNO=(SELECT TOP(1) COLOR FROM BANDDEFINITION WHERE TORANGE>= DATEDIFF(DAY,EXPIRYDATE,GETDATE()) ORDER BY TORANGE)
--WHERE WAREHOUSEID IN (SELECT WAREHOUSEID FROM WAREHOUSE WHERE WAREHOUSETYPEID=1)

DECLARE VLOOP CURSOR FOR
SELECT WAREHOUSEID,WAREHOUSECODE FROM WAREHOUSE WHERE WAREHOUSETYPEID=1 and WarehouseCode collate SQL_Latin1_General_CP1256_CI_AS in (select Warehouse_Code from Cubita_test.dbo.InCube_Warehouses_stocks)
OPEN VLOOP
FETCH NEXT FROM VLOOP INTO @VEHICLEID,@VEHICLECODE
WHILE (@@FETCH_STATUS=0)
BEGIN

DELETE FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@VEHICLEID

DECLARE PLOOP CURSOR FOR
SELECT W.WAREHOUSEID,P.PACKID,CS.qty,I.ITEMID,''1990-01-01'' INVENTBATCHID,''1990-01-01 00:00:00.000''EXPDATE
FROM Cubita_test.dbo.InCube_Warehouses_stocks CS
INNER JOIN WAREHOUSE W ON CS.WAREHOUSE_CODE=W.WAREHOUSECODE collate SQL_Latin1_General_CP1256_CI_AS
INNER JOIN ITEM I ON I.ITEMCODE collate SQL_Latin1_General_CP1256_CI_AS=CS.ITEMCODE --and i.Origin=cs.flag
inner join packtypelanguage ptl on ptl.description collate SQL_Latin1_General_CP1256_CI_AS=cs.uom
inner join pack p on p.itemid=i.itemid and p.PackTypeID=ptl.PackTypeID
WHERE W.WAREHOUSEID=@VEHICLEID and I.Inactive=0
OPEN PLOOP
FETCH NEXT FROM PLOOP INTO @WAREHOUSEID , @PACKID ,@QUANTITY,@ITEMID,@BATCHNO,@EXPDATE
WHILE(@@FETCH_STATUS=0)
BEGIN

----------------------------------------

DECLARE LOOP2 CURSOR FOR
SELECT PACKID FROM PACK WHERE ITEMID=@ITEMID AND PACKID<>@PACKID
OPEN LOOP2
FETCH NEXT FROM LOOP2 INTO @PACKID2
WHILE (@@FETCH_STATUS=0)
BEGIN
IF EXISTS(SELECT * FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID2 AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE)
BEGIN
UPDATE WAREHOUSESTOCK SET QUANTITY=QUANTITY+0, BASEQUANTITY= BASEQUANTITY+0 WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID2 AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE
END
ELSE
BEGIN
INSERT INTO WAREHOUSESTOCK
SELECT @WAREHOUSEID,1,@PACKID2,@EXPDATE,@BATCHNO,0,NULL,0,0,-1
END

FETCH NEXT FROM LOOP2 INTO @PACKID2
END
CLOSE LOOP2
DEALLOCATE LOOP2

IF EXISTS(SELECT * FROM WAREHOUSESTOCK WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE)
BEGIN
UPDATE WAREHOUSESTOCK SET QUANTITY=QUANTITY+@QUANTITY, BASEQUANTITY= BASEQUANTITY+@QUANTITY WHERE WAREHOUSEID=@WAREHOUSEID AND PACKID=@PACKID AND BATCHNO=@BATCHNO AND EXPIRYDATE=@EXPDATE
END
ELSE
BEGIN
INSERT INTO WAREHOUSESTOCK
SELECT @WAREHOUSEID,1,@PACKID,@EXPDATE,@BATCHNO,@QUANTITY,NULL,0,@QUANTITY,-1
END

----------------------------------------

FETCH NEXT FROM PLOOP INTO @WAREHOUSEID , @PACKID ,@QUANTITY,@ITEMID,@BATCHNO,@EXPDATE
END
CLOSE PLOOP
DEALLOCATE PLOOP

FETCH NEXT FROM VLOOP INTO @VEHICLEID,@VEHICLECODE
END
CLOSE VLOOP
DEALLOCATE VLOOP



COMMIT TRANSACTION T1
END try
BEGIN CATCH
ROLLBACK TRANSACTION T1
SELECT ERROR_NUMBER(), ''sp_UpdateStock'',ERROR_LINE(),ERROR_MESSAGE(),GETDATE()
 
END CATCH
end
')
