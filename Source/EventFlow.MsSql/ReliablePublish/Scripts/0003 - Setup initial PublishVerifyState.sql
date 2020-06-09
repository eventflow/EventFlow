IF NOT EXISTS (SELECT * FROM [dbo].[EventFlowPublishVerifyState])
BEGIN
    INSERT INTO [dbo].[EventFlowPublishVerifyState] (LastVerifiedPosition) VALUES ('')
END
