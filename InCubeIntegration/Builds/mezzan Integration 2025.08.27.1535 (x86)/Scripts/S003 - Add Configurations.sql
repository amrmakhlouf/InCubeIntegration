IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'SiteSymbol')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (4,'SiteSymbol','')
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (5,'ConditionalSymbol','')
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'LoginRequired')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (6,'LoginRequired','true')
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'OrganizationOriented')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (7,'OrganizationOriented','false')
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WindowsServiceEnabled')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (8,'WindowsServiceEnabled','true')
END