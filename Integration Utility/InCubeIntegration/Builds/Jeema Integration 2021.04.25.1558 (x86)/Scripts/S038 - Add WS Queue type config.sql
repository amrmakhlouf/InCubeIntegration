IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Queues_Mode')
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly) VALUES (14,'WS_Queues_Mode','2',-1,3,1)
END