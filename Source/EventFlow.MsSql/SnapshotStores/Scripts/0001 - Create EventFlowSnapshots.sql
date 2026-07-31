IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = N'$MsSqlSchema$'  AND table_name = N'EventFlowSnapshots')
BEGIN
	CREATE TABLE [$MsSqlSchema$].[EventFlowSnapshots](
		[Id] [bigint] IDENTITY(1,1) NOT NULL,
		[AggregateId] [nvarchar](128) NOT NULL,
		[AggregateName] [nvarchar](128) NOT NULL,
		[AggregateSequenceNumber] [int] NOT NULL,
		[Data] [nvarchar](MAX) NOT NULL,
		[Metadata] [nvarchar](MAX) NOT NULL,
		CONSTRAINT [PK_EventFlowSnapshots] PRIMARY KEY CLUSTERED
		(
			[Id] ASC
		)
	)

	CREATE UNIQUE NONCLUSTERED INDEX [IX_EventFlowSnapshots_AggregateId_AggregateSequenceNumber] ON [$MsSqlSchema$].[EventFlowSnapshots]
	(
		[AggregateName] ASC,
		[AggregateId] ASC,
		[AggregateSequenceNumber] ASC
	)
END
