CREATE TABLE [EventFlow](
	[GlobalSequenceNumber] [INTEGER] PRIMARY KEY ASC NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
	[AggregateId] [nvarchar](255) NOT NULL,
	[AggregateName] [nvarchar](255) NOT NULL,
	[Data] [nvarchar](1024) NOT NULL,
	[Metadata] [nvarchar](1024) NOT NULL,
	[AggregateSequenceNumber] [int] NOT NULL
);

CREATE UNIQUE INDEX [IX_EventFlow_AggregateId_AggregateSequenceNumber] ON [EventFlow](
	[AggregateId] ASC,
	[AggregateSequenceNumber] ASC
);