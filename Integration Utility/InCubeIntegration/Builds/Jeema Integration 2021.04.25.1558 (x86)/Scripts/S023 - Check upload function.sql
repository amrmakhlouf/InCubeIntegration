IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'IsRouteHistoryUploaded')
BEGIN
EXEC(
'CREATE FUNCTION dbo.IsRouteHistoryUploaded (@RouteHisoryID INT) RETURNS BIT
AS
BEGIN

DECLARE @Uploaded BIT, @MaxRouteHistoryID INT
SET @Uploaded = 0
SET @MaxRouteHistoryID = 0

SELECT @MaxRouteHistoryID = MAX(RouteHistoryID) FROM RouteHistory WHERE EmployeeID IN (
SELECT EmployeeID FROM RouteHistory WHERE RouteHistoryID = @RouteHisoryID)

IF (@RouteHisoryID = @MaxRouteHistoryID)
	SELECT @Uploaded = Uploaded FROM RouteHistory WHERE RouteHistoryID = @RouteHisoryID

RETURN @Uploaded;

END
')
END