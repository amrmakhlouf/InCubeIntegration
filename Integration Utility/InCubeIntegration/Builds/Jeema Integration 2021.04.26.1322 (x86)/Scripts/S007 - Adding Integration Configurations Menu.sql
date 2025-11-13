IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 9 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (9,1,'Integration Configurations',0)
END