UPDATE Int_Configuration SET KeyValue = 'pepsipal' WHERE KeyName = 'SiteSymbol'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'LoginRequired'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'WindowsServiceEnabled'


IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_UserName' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'WS_UserName','invan',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Password' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1004,'WS_Password','abc@123',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_URL' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'WS_URL','https://gw.bisan.com/api/odemo_6',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SSL_File' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'SSL_File','khaled.pfx',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SSL_Key' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'SSL_Key','bisan901',-1,1)

/*
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 5)
BEGIN
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1001,'AppServerHost','10.80.64.104',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Name' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1002,'Name','UAEP',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'User' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'User','rfcuser',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Password' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1004,'Password','Delmonte#252525',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Client' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1005,'Client','350',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Language' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1006,'Language','EN',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemNumber' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1007,'SystemNumber','00',5,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemID' AND OrganizationID = 5)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1008,'SystemID','E09',5,1)
END
*/



INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (1, 1, N'sp_UpdateItems', 1, N'Stg_Items', 1, N'SELECT ID , code + '': '' + Message ')
GO
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (2, 1, N'sp_UpdateCustomers', 1, N'stg_customers', 1, N'select distinct Message from stg_customers where Skipped=1 ')
GO
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (3, 1, N'sp_UpdatePrices', 1, N'Stg_Prices', 1, N'select distinct Message from Stg_Prices where Skipped=1 ')
GO
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (10, 1, N'sp_UpdateStock', 1, N'sp_UpdateStock', 1, N'select distinct Message from sp_UpdateStock where Skipped=1 and Message like ''%van%''')
GO
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (20, 1, N'sp_UpdateWarehouse', 1, N'Stg_Warehouse', 1, N'select distinct Message from Stg_Warehouse where Skipped=1 ')
GO
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (21, 1, N'sp_UpdateSalespersons', 1, N'Stg_Salespersons', 1, N'select distinct Message from Stg_Salespersons where Skipped=1 ')
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (1, 1, N'@TriggerID', N'@TriggerID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (2, 1, N'@TriggerID', N'@TriggerID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (3, 1, N'@TriggerID', N'@TriggerID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (10, 1, N'@TriggerID', N'@TriggerID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (10, 1, N'@WarehouseID', N'@WarehouseID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (20, 1, N'@TriggerID', N'@TriggerID', 1)
GO
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (21, 1, N'@TriggerID', N'@TriggerID', 1)
GO
