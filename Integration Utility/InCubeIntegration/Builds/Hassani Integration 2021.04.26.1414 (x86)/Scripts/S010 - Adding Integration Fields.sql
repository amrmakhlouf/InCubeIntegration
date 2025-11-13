IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 29 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (29,1,'Promotion');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 29 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (29,2,7);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 30 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (30,2,'Stock Interface');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 30 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (30,2,8);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 31 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (31,2,'Invocie Interface');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 31 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (31,2,8);
END