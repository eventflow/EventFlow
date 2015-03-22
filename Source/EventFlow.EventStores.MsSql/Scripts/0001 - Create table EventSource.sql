CREATE TABLE [dbo].[EventFlow](
	[GlobalSequenceNumber] [bigint] IDENTITY(1,1) NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
	[AggregateId] [nvarchar](255) NOT NULL,
	[AggregateName] [nvarchar](255) NOT NULL,
	[Data] [nvarchar](max) NOT NULL,
	[Metadata] [nvarchar](max) NOT NULL,
	[AggregateSequenceNumber] [int] NOT NULL,
	CONSTRAINT [PK_EventSource] PRIMARY KEY CLUSTERED 
	(
		[GlobalSequenceNumber] ASC
	)
)

CREATE NONCLUSTERED INDEX [IX_EventSource_AggregateId_AggregateSequenceNumber] ON [dbo].[EventSource]
(
	[AggregateId] ASC,
	[AggregateSequenceNumber] ASC
)
