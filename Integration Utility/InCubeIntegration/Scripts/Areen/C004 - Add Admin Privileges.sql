IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,1,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 7 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,7,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 8 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,8,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,1,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,2,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,3,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 17 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,17,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 4 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,4,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 18 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,18,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 10 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,10,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 23 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,23,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 12 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,12,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 24 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,24,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 11 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Value) VALUES (0,11,2,1)
END