IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 32 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (32,1,'Orders');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 32 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (32,1,7);
END