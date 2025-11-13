UPDATE Int_Field SET ActionType = 3, FieldName = 'Export Images' WHERE FieldID = 45
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 45 AND FilterID = 6)
BEGIN
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (45,6);
END
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 45 AND FilterID = 7)
BEGIN
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (45,7);
END
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 17 AND FilterID = 6)
BEGIN
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (17,6);
END
IF NOT EXISTS (SELECT * FROM Int_FieldFilters WHERE FieldID = 17 AND FilterID = 7)
BEGIN
	INSERT INTO Int_FieldFilters (FieldID,FilterID) VALUES (17,7);
END
