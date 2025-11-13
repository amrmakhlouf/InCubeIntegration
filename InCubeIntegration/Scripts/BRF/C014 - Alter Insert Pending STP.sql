EXEC('
ALTER PROCEDURE [dbo].[sp_InsertPendingSTP]
AS
BEGIN

DELETE FROM PendingSTP WHERE SendingDate < DATEADD(DD,-7,GETDATE());

INSERT INTO PendingSTP
--SELECT DISTINCT E.Email,H.OrderID,H.AttemptDate
--FROM OrderSyncHistory H
--INNER JOIN SalesOrder SO ON SO.OrderID = H.OrderID
--INNER JOIN Employee E ON SO.EmployeeID = E.EmployeeID
--LEFT JOIN STPDetails S ON S.OrderNo = H.OrderID
--LEFT JOIN PendingSTP P ON P.OrderNo = H.OrderID
--WHERE Result = ''Success'' AND AttemptDate > DATEADD(DD,-7,GETDATE())
--AND SO.RecievedBOTime > DATEADD(DD,-7,GETDATE())
--AND P.OrderNo IS NULL AND (S.OrderNo IS NULL OR S.SendingDate < DATEADD(DD,-7,GETDATE()))
--AND H.OrderID <> '''';

SELECT E.Email,ED.Filter1Value,ED.RunTimeStart
FROM Int_ExecutionDetails ED
INNER JOIN Int_ActionTrigger AT ON AT.ID = ED.TriggerID
INNER JOIN SalesOrder SO ON SO.OrderID = ED.Filter1Value AND CAST(SO.CustomerID AS NVARCHAR(20)) = ED.Filter2Value
AND CAST(SO.OutletID AS NVARCHAR(10)) = ED.Filter3Value 
INNER JOIN Employee E ON E.EmployeeID = SO.EmployeeID
LEFT JOIN STPDetails S ON S.OrderNo = SO.OrderID AND S.MailBox = E.Email
LEFT JOIN PendingSTP P ON P.OrderNo = SO.OrderID AND P.MailBox = E.Email
WHERE AT.FieldID = 11 AND ED.ResultID = 1 AND ED.RunTimeStart > DATEADD(DD,-7,GETDATE())
AND SO.RecievedBOTime > DATEADD(DD,-7,GETDATE())
AND P.OrderNo IS NULL AND (S.OrderNo IS NULL OR S.SendingDate < DATEADD(DD,-7,GETDATE()))


END
')