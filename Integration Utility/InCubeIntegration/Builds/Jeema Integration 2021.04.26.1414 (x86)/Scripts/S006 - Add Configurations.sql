UPDATE Int_Configuration SET DataType = 2 WHERE ConfigurationID IN (6,7,8)
UPDATE Int_Configuration SET DataType = 3 WHERE ConfigurationID IN (1,2,3)

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormTitle')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType) VALUES (9,'IntegrationFormTitle','InVan Integration Utility',1)
END

IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'IntegrationFormBackColor')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,DataType) VALUES (10,'IntegrationFormBackColor','-4007433',4)
END