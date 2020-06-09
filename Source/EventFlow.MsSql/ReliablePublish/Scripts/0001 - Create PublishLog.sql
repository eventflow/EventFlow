IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = N'dbo'  AND table_name = N'EventFlowPublishLog')
BEGIN
	CREATE TABLE [dbo].[EventFlowPublishLog](
		[Id] [bigint] IDENTITY(1,1) NOT NULL,
		[AggregateId] [nvarchar](128) NOT NULL,
        [MinAggregateSequenceNumber] [int] NOT NULL,
		[MaxAggregateSequenceNumber] [int] NOT NULL,
		CONSTRAINT [PK_EventFlowPublishLog] PRIMARY KEY CLUSTERED
		(
			[Id] ASC
		)
	)
END
