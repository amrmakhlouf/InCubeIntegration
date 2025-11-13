IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 10 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (10,1,'Load Request Import',0)
END