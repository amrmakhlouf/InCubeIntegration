IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 40 AND ActionType = 1)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (40,1,'Bank');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 40 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (40,2,7);
END
