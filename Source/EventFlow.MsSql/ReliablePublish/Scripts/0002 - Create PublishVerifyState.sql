IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = N'dbo'  AND table_name = N'EventFlowPublishVerifyState')
BEGIN
	CREATE TABLE [dbo].[EventFlowPublishVerifyState](
		[Id] [bigint] IDENTITY(1,1) NOT NULL,
		[LastVerifiedPosition] [nvarchar](255) NOT NULL,
		CONSTRAINT [PK_EventFlowPublishVerifyState] PRIMARY KEY CLUSTERED
		(
			[Id] ASC
		)
	)
END
