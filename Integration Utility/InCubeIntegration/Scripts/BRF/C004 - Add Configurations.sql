IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_PROD')
BEGIN
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_URL')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1001,'WS_URL','http://spouxp2p.perdigao.com.br:50200/XISOAPAdapter/MessageServlet?channel=:BS_PALM_05:CC_PALM_SOAP_SND&amp;version=3.0&amp;Sender.Service=CC_PALM_SOAP_SND&amp;Interface=ITF_O_S_EnvRecArqPalm%5EBS_PALM_05')
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_UserName')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1002,'WS_UserName','PIINCUBE')
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Password')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1003,'WS_Password','brf2608@')
END

IF EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'ConditionalSymbol' AND KeyValue = 'BRF_QAS')
BEGIN
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_URL')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1001,'WS_URL','http://web.brasilfoods.com:8101/XISOAPAdapter/MessageServlet?channel=:BS_PALM_05:CC_PALM_SOAP_SND&amp;version=3.0&amp;Sender.Service=CC_PALM_SOAP_SND&amp;Interface=ITF_O_S_EnvRecArqPalm%5EBS_PALM_05')
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_UserName')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1002,'WS_UserName','PILEGADOS')
IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Password')
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue) VALUES (1003,'WS_Password','P3RD1G@0')
END

