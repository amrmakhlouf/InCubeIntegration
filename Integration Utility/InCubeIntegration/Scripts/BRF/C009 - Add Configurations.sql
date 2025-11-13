IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormTitle' AND OrganizationID = 1)
BEGIN
	IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_PROD')
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','InVan Integration Utility - KSA',1,1)
	ELSE
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','QAS - KSA',1,1)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormBackColor' AND OrganizationID = 1)
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (10,'IntegrationFormBackColor','-16724992',4,1)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormTitle' AND OrganizationID = 2)
BEGIN
	IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_PROD')
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','InVan Integration Utility - UAE',1,2)
	ELSE
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','QAS- UAE',1,2)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormBackColor' AND OrganizationID = 2)
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (10,'IntegrationFormBackColor','-7675930',4,2)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormTitle' AND OrganizationID = 5)
BEGIN
	IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_PROD')
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','InVan Integration Utility - Kuwait',1,5)
	ELSE
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','QAS - Kuwait',1,5)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormBackColor' AND OrganizationID = 5)
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (10,'IntegrationFormBackColor','-36495',4,5)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormTitle' AND OrganizationID = 6)
BEGIN
	IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_PROD')
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','InVan Integration Utility - Qatar',1,6)
	ELSE
		INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (9,'IntegrationFormTitle','QAS - Qatar',1,6)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormBackColor' AND OrganizationID = 6)
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType,OrganizationID) VALUES (10,'IntegrationFormBackColor','-1321411',4,6)
END