IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 42 AND PrivilegeType = 2)
BEGIN
	INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,42,2,4);
END

