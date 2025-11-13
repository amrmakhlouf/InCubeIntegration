IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 41 AND ActionType = 3)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (41,3,'Database Actions');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 41 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (41,2,-1);
END

UPDATE Int_Privileges SET PrivilegeType = 2, ParentID = -1 WHERE PrivilegeID = 33;
