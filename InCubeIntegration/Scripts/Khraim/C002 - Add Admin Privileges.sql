IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,1,1)
END
 
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,3,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 7 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,7,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 8 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,8,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 10 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,10,2,1)
END 
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 21 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,21,2,2)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,2,3)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,3,2,4)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 27 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,27,2,1)
END


IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 20 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,20,2,1)
END
--IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 40 AND PrivilegeType = 2)
--BEGIN
--INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,40,2,1)
--END


--IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 12 AND PrivilegeType = 2)
--BEGIN
--INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,12,2,1)
--END


IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 11 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,11,2,1)
END 

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 6 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,6,2,1)
END 

