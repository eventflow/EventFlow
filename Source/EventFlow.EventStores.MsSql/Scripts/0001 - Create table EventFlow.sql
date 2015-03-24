CREATE TABLE [dbo].[EventFlow](
	[GlobalSequenceNumber] [bigint] IDENTITY(1,1) NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
	[AggregateId] [nvarchar](255) NOT NULL,
	[AggregateName] [nvarchar](255) NOT NULL,
	[Data] [nvarchar](max) NOT NULL,
	[Metadata] [nvarchar](max) NOT NULL,
	[AggregateSequenceNumber] [int] NOT NULL,
	CONSTRAINT [PK_EventFlow] PRIMARY KEY CLUSTERED
	(
		[GlobalSequenceNumber] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_EventFlow_AggregateId_AggregateSequenceNumber] ON [dbo].[EventFlow]
(
	[AggregateId] ASC,
	[AggregateSequenceNumber] ASC
)
