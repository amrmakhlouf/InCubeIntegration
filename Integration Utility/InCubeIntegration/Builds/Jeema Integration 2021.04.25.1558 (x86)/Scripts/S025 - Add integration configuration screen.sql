IF NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'ReadOnly' AND object_id IN (SELECT object_id FROM sys.tables WHERE name = 'Int_Configuration'))
BEGIN
	ALTER TABLE Int_Configuration ADD ReadOnly BIT NULL
END

EXEC ('UPDATE Int_Configuration SET ReadOnly = 0')
EXEC ('UPDATE Int_Configuration SET ReadOnly = 1 WHERE KeyName IN (
''ConditionalSymbol'',
''AppVersion'',
''ClientVersion'',
''DBVersion'',
''LoginRequired'',
''OrganizationOriented'',
''SiteSymbol'',
''WindowsServiceEnabled'')')
