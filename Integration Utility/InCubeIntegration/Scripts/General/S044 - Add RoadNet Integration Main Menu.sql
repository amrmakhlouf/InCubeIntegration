IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 17 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (17,1,'Export Orders To RaodNet',16)
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 18 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (18,1,'Import Sessions From RoadNet',16)
END

IF NOT EXISTS (SELECT * FROM Int_Privileges WHERE PrivilegeID = 19 AND PrivilegeType = 1)
BEGIN
	INSERT INTO Int_Privileges (PrivilegeID,PrivilegeType,Description,ParentID) VALUES (19,1,'Configure Route Region',16)
END