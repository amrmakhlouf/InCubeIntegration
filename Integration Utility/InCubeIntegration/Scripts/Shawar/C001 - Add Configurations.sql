UPDATE Int_Configuration SET KeyValue = 'shawar' WHERE KeyName = 'SiteSymbol';
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'LoginRequired';
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'WindowsServiceEnabled';
update Organization set OrganizationCode='shawar' where Organizationid=1;



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

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 40 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (40, 1, N'sp_UpdateBank', 1, N'Stg_Bank', 1, N'select distinct Message from Stg_Bank where Skipped=1 ')

IF NOT EXISTS (SELECT * FROM [Int_FieldProcedure] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcedure] ([FieldID], [Sequence], [ProcedureName], [ProcedureType], [ExecutionTableName], [ReadExecutionDetails], [ExecDetailsReadQry]) VALUES (6, 1, N'sp_UpdateInvoices', 1, N'Stg_Invoices', 0, N'select distinct Message from Stg_Invoices where Skipped=1 ')

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

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 40 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (40, 1, N'@TriggerID', N'@TriggerID', 1)

IF NOT EXISTS (SELECT * FROM [Int_FieldProcParams] WHERE [FieldID] = 6 AND [Sequence]=1)
INSERT [dbo].[Int_FieldProcParams] ([FieldID], [Sequence], [ParameterName], [ParameterValue], [ParameterType]) VALUES (6, 1, N'@TriggerID', N'@TriggerID', 1)

