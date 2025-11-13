IF NOT EXISTS (SELECT * FROM Int_Field WHERE FieldID = 42 AND ActionType = 2)
BEGIN
	INSERT INTO Int_Field (FieldID,ActionType,FieldName) VALUES (42,2,'CN Request');
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 42 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,ParentID) VALUES (42,2,8);
END

IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 42 AND FilterID = 6)
	INSERT INTO Int_FieldFilters VALUES (42,6)
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 42 AND FilterID = 7)
	INSERT INTO Int_FieldFilters VALUES (42,7)