DECLARE @TaskID INT, @ActionID INT, @FilterID INT, @IDs nvarchar(max), @Name nvarchar(200), @GroupID INT, @TransferID INT, @Seq INT, @Index INT

DECLARE LOOP1 CURSOR STATIC FOR
SELECT AF.*,T.Name
FROM Int_ActionFilter AF
INNER JOIN Int_Tasks T ON T.TaskID = AF.TaskID
INNER JOIN Int_TaskAction TA ON TA.TaskID = AF.TaskID AND TA.ActionID = AF.ActionID
WHERE T.Status IN (1,2) AND TA.FieldID = 43;

OPEN LOOP1
FETCH NEXT FROM LOOP1
INTO @TaskID,@ActionID,@FilterID,@IDs,@Name

WHILE(@@FETCH_STATUS=0)
BEGIN
SELECT @GroupID = ISNULL(MAX(GroupID),0)+1 FROM Int_DataTrasferGroups;
INSERT INTO Int_DataTrasferGroups (GroupID,GroupName) VALUES (@GroupID,@Name);
SET @Seq = 1;
SET @Index = PATINDEX('%,%', @IDs)
	WHILE (@Index <> 0)
	BEGIN
		SET @TransferID = LTRIM(RTRIM(SUBSTRING(@IDs,1,@Index-1)));
		SET @IDs = SUBSTRING(@IDs,@Index+1,LEN(@IDs)-@Index);
		INSERT INTO Int_DataTrasferGroupDetails (GroupID,TransferTypeID,Sequence) VALUES (@GroupID,@TransferID,@Seq);
		SET @Seq = @Seq + 1;
		SET @Index = PATINDEX('%,%', @IDs)
	END
	SET @TransferID = @IDs;
	INSERT INTO Int_DataTrasferGroupDetails (GroupID,TransferTypeID,Sequence) VALUES (@GroupID,@TransferID,@Seq);
	UPDATE Int_ActionFilter SET Value = @GroupID WHERE TaskID = @TaskID AND ActionID = @ActionID AND FilterID = @FilterID
FETCH NEXT FROM LOOP1
INTO @TaskID,@ActionID,@FilterID,@IDs,@Name
END

CLOSE LOOP1
DEALLOCATE LOOP1