IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 34 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (34,1,'New Customer');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 34 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (34,2,7);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 35 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (35,1,'Target');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 35 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (35,2,7);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 36 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (36,1,'POSM');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 36 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (36,2,7);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 37 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (37,1,'Contracted FOC');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 37 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (37,2,7);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 38 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (38,2,'Price');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 38 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (38,2,8);
END

IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 39 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (39,2,'Promotion');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 39 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (39,2,8);
END