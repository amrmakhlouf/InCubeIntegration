IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 28 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (28,2,'Down Payments');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 28 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (28,2,8);
END