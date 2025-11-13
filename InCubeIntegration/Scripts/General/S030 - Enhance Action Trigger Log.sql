IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'MainLoopQuery' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_ActionTrigger'))
BEGIN
	ALTER TABLE Int_ActionTrigger ADD MainLoopQuery NVARCHAR(max) NULL
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'InboundStagingDB')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly) 
VALUES (12,'InboundStagingDB','',-1,1,0)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'OutboundStagingDB')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly) 
VALUES (13,'OutboundStagingDB','',-1,1,0)
END

EXEC ('ALTER TABLE ERROR_TRACK ALTER COLUMN ErrorDate DateTime') 