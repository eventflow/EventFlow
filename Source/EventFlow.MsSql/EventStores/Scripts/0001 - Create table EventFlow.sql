IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = N'dbo'  AND table_name = N'EventFlow')
BEGIN
	CREATE TABLE [dbo].[EventFlow](
		[GlobalSequenceNumber] [bigint] IDENTITY(1,1) NOT NULL,
		[BatchId] [uniqueidentifier] NOT NULL,
		[AggregateName] [nvarchar](255) NOT NULL,
		[AggregateId] [nvarchar](255) NOT NULL,
		[AggregateSequenceNumber] [int] NOT NULL,
		[Data] [nvarchar](max) NOT NULL,
		[Metadata] [nvarchar](max) NOT NULL,
		CONSTRAINT [PK_EventFlow] PRIMARY KEY CLUSTERED
		(
			[GlobalSequenceNumber] ASC
		)
	)

	CREATE UNIQUE NONCLUSTERED INDEX [IX_EventFlow_AggregateName_AggregateId_AggregateSequenceNumber] ON [dbo].[EventFlow]
	(
		[AggregateName] ASC,
		[AggregateId] ASC,
		[AggregateSequenceNumber] ASC
	)
END