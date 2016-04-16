CREATE TABLE [dbo].[EventFlowSnapshots](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[AggregateId] [nvarchar](128) NOT NULL,
	[AggregateName] [nvarchar](128) NOT NULL,
	[AggregateSequenceNumber] [int] NOT NULL,
	[Data] [nvarchar](MAX) NOT NULL,
	[Metadata] [nvarchar](MAX) NOT NULL,
	CONSTRAINT [PK_EventFlow] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_EventFlow_AggregateId_AggregateSequenceNumber] ON [dbo].[EventFlow]
(
	[AggregateId] ASC,
	[AggregateSequenceNumber] ASC
)