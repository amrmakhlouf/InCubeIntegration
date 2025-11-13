UPDATE Int_Configuration SET KeyValue = 'delmonte' WHERE KeyName = 'SiteSymbol'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'LoginRequired'
UPDATE Int_Configuration SET KeyValue = 'true' WHERE KeyName = 'WindowsServiceEnabled'

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 2)
BEGIN
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1001,'AppServerHost','10.80.67.58',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Name' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1002,'Name','UAEP',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'User' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'User','rfcuser',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Password' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1004,'Password','Delmonte#252525',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Client' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1005,'Client','400',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Language' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1006,'Language','EN',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemNumber' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1007,'SystemNumber','00',2,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemID' AND OrganizationID = 2)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1008,'SystemID','E04',2,1)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 4)
BEGIN
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'AppServerHost' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1001,'AppServerHost','10.80.67.48',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Name' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1002,'Name','KSAP',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'User' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1003,'User','rfcuser',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Password' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1004,'Password','Delmonte#252525',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Client' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1005,'Client','500',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'Language' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1006,'Language','EN',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemNumber' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1007,'SystemNumber','00',4,1)
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SystemID' AND OrganizationID = 4)
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType) VALUES (1008,'SystemID','E01',4,1)
END