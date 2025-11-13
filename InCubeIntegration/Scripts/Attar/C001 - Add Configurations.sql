UPDATE Int_Configuration SET KeyValue = 'attar' WHERE KeyName = 'SiteSymbol'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'LoginRequired'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'WindowsServiceEnabled'


IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_UserName' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,Readonly) VALUES (1003,'WS_UserName','api_attar',-1,1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Password' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,Readonly) VALUES (1004,'WS_Password','RQ2kIW6b',-1,1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_URL' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,Readonly) VALUES (1005,'WS_URL','HTTP://192.168.1.90:8282/api/rest',-1,1,1) 

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



IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 1 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (1, 1, N'sp_UpdateItems', 1, N'Stg_Items', 1, N'SELECT ID , code + '': '' + Message ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 2 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (2, 1, N'sp_UpdateCustomers', 1, N'stg_customers', 1, N'select distinct Message from stg_customers where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 3 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (3, 1, N'sp_UpdatePrices', 1, N'Stg_Prices', 1, N'select distinct Message from Stg_Prices where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 10 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (10, 1, N'sp_UpdateStock', 1, N'sp_UpdateStock', 1, N'select distinct Message from sp_UpdateStock where Skipped=1 and Message like ''%van%''')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 20 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (20, 1, N'sp_UpdateWarehouse', 1, N'Stg_Warehouse', 1, N'select distinct Message from Stg_Warehouse where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 21 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (21, 1, N'sp_UpdateSalespersons', 1, N'Stg_Salespersons', 1, N'select distinct Message from Stg_Salespersons where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (6, 1, N'sp_UpdateInvoices', 1, N'stg_Invoices', 1, N'select distinct Message from stg_Invoices where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 17 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (17, 1, N'sp_UpdateCustomerBalances', 1, N'Stg_CustomerBalances', 1, N'select distinct Message from Stg_CustomerBalances where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 1 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (1, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 2 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (2, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 3 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (3, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 10 AND [Sequence]=1  AND [ParameterName]= N'@TriggerID')
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (10, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 10 AND [Sequence]=1 AND [ParameterName]= N'@WarehouseID')
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (10, 1, N'@WarehouseID', N'@WarehouseID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 20 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (20, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 21 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (21, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (6, 1, N'@TriggerID', N'@TriggerID', 1)
IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 47 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (47, 1, N'sp_UpdateAreas', 1, N'Stg_Areas',0, N'')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 47 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (47, 1, N'@TriggerID', N'@TriggerID', 1)
IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 17 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (17, 1, N'@TriggerID', N'@TriggerID', 1)
