IF NOT EXISTS (SELECT * FROM Int_Configuration WHERE KeyName = 'WS_Machine_Name')
BEGIN
	INSERT INTO Int_Configuration (ConfigurationID,KeyName,KeyValue,OrganizationID,DataType,ReadOnly) VALUES (15,'WS_Machine_Name','',-1,1,1)
END

DECLARE @MachineName NVARCHAR(100) = '';
SELECT TOP 1 @MachineName = MachineName FROM Int_Session WHERE UserID = -1 ORDER BY SessionID DESC;
UPDATE Int_Configuration SET KeyValue = @MachineName WHERE ConfigurationID = 15;
