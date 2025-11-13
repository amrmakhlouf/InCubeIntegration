IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 16 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (16,1,'RoadNet Integration',0)
END