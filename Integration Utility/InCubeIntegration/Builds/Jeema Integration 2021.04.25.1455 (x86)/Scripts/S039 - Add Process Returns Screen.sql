IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 15 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (15,1,'Process Returns',0)
END