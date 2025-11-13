UPDATE Int_Configuration SET KeyValue = 'anabtawi' WHERE KeyName = 'SiteSymbol'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'LoginRequired'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'WindowsServiceEnabled'


IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_UserName' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'WS_UserName','cube2',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Password' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1004,'WS_Password','att12345678',-1,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_URL' AND OrganizationID = -1)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'WS_URL','https://gw.bisan.com/api/odemo_2',-1,1)
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