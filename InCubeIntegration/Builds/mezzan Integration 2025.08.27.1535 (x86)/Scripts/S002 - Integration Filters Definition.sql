--1: Organization
--2: Employee
--3: Customer
--4: Warehouse
--5: StockDate
--6: FromDate
--7: ToDate

DELETE FROM Int_Filter;

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,QueryString,ValueMember,DisplayMember1,DisplayMember2,DisplayDescription1,DisplayDescription2)
VALUES ('Organization',5,0,'-1',
'SELECT O.OrganizationID, O.OrganizationCode, OL.Description
FROM Organization O
INNER JOIN OrganizationLanguage OL ON OL.OrganizationID = O.OrganizationID
AND OL.LanguageID = @Language
WHERE O.OrganizationID IN (@Organization)'
,'OrganizationID','Description','OrganizationCode','Organization Name','Organization Code')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,QueryString,ValueMember,DisplayMember1,DisplayMember2,DisplayDescription1,DisplayDescription2)
VALUES ('Employee',5,1,'-1',
'SELECT E.EmployeeID, E.EmployeeCode, E.Email + '' - '' + EL.Description Description
FROM Employee E
INNER JOIN EmployeeLanguage EL ON EL.EmployeeID = E.EmployeeID
AND EL.LanguageID = @Language
WHERE E.OrganizationID IN (@Organization)'
,'EmployeeID','Description','EmployeeCode','Employee Name','Employee Code')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,QueryString,ValueMember,DisplayMember1,DisplayMember2,DisplayDescription1,DisplayDescription2)
VALUES ('Customer',5,1,'-1',
'SELECT CO.CustomerID, CO.CustomerCode, COL.Description
FROM CustomerOutlet CO
INNER JOIN CustomerOutletLanguage COL ON COL.CustomerID = CO.CustomerID AND COl.OutletID = CO.OutletID 
AND COL.LanguageID = @Language
WHERE CO.OrganizationID IN (@Organization)'
,'CustomerID','Description','CustomerCode','Customer Name','Customer Code')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,QueryString,ValueMember,DisplayMember1,DisplayMember2,DisplayDescription1,DisplayDescription2)
VALUES ('Warehouse',5,1,'-1',
'SELECT W.WarehouseID, W.WarehouseCode, WL.Description
FROM Warehouse W
INNER JOIN WarehouseLanguage WL ON WL.WarehouseID = W.WarehouseID
AND WL.LanguageID = @Language AND W.WarehouseTypeID = 1
WHERE W.OrganizationID IN (@Organization)'
,'WarehouseID','Description','WarehouseCode','Warehouse Name','Warehouse Code')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,DisplayDescription1)
VALUES ('StockDate',1,0,'@Today','Stock Date')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,DisplayDescription1)
VALUES ('FromDate',1,1,'@Today','From Date')

INSERT INTO Int_Filter (FilterName,FilterType,CanSelectAll,DefaultValue,DisplayDescription1)
VALUES ('ToDate',1,1,'@Today','To Date')