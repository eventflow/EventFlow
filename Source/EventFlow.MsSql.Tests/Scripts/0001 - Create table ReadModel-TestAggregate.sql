CREATE TABLE [dbo].[ReadModel-TestAggregate](
	[PingsReceived] [int] NOT NULL,
	[DomainErrorAfterFirstReceived] [bit] NOT NULL,

	-- -------------------------------------------------
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[AggregateId] [nvarchar](64) NOT NULL,
	[CreateTime] [datetimeoffset](7) NOT NULL,
	[UpdatedTime] [datetimeoffset](7) NOT NULL,
	[LastAggregateSequenceNumber] [int] NOT NULL,
	[LastGlobalSequenceNumber] [bigint] NOT NULL,
	CONSTRAINT [PK_ReadModel-TestAggregate] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-TestAggregate_AggregateId] ON [dbo].[ReadModel-TestAggregate]
(
	[AggregateId] ASC
)
