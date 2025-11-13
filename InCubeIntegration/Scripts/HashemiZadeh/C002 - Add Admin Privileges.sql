--Manual Integration
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,1,1)
END

--Users access
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,3,1,2)
END

--Schedules Management
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,1,3)
END

--Update
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 7 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,7,1,1)
END

--Send
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 8 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,8,1,1)
END

--Items
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,2,1)
END

--Employees
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 21 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,21,2,2)
UPDATE Int_Field SET FieldName = 'Employees' WHERE FieldID = 21
END

--Customers
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,2,3)
END

--Outstanding
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 17 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,17,2,4)
END

--KPI
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 7 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,7,2,5)
END

--Stock
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 10 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,10,2,6)
END

--Sales
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 23 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,23,2,1)
END

--Returns
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 25 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,25,2,2)
END

--Transfers
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 24 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,24,2,3)
END