IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,1,1)
END
 
IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,3,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 7 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,7,1,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 8 AND PrivilegeType = 1)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,8,1,1)
END

--IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 10 AND PrivilegeType = 1)
--BEGIN
--INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,10,1,1)
--END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 1 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,1,2,1)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 21 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,21,2,2)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 2 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,2,2,3)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 3 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,3,2,4)
END

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 23 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,23,2,1)
END


IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 20 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,20,2,1)
END


IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 12 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,12,2,1)
END




IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 17 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,17,2,1)
END
IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 17 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (17, 1, N'sp_UpdateCustomerBalances', 1, N'Stg_CustomerBalances', 1, N'select distinct Message from Stg_CustomerBalances where Skipped=1 ')


IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 17 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (17, 1, N'@TriggerID', N'@TriggerID', 1)



 IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 6 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,6,2,1)
END
IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (6, 1, N'sp_UpdateInvoices', 1, N'stg_Invoices', 1, N'select distinct Message from stg_Invoices where Skipped=1 ')
IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (6, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM Int_UserPrivileges WHERE UserID = 0 AND PrivilegeID = 11 AND PrivilegeType = 2)
BEGIN
INSERT INTO Int_UserPrivileges (UserID,PrivilegeID,PrivilegeType,Sequence) VALUES (0,11,2,1)
END 