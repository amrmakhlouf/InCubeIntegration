IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'DefaultMenu')
BEGIN
INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly) VALUES (11,'DefaultMenu','1',-1,3,1)
END
