CREATE TABLE [dbo].[ReadModel-ThingyAggregate](
	[PingsReceived] [int] NOT NULL,
	[DomainErrorAfterFirstReceived] [bit] NOT NULL,

	-- -------------------------------------------------
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[AggregateId] [nvarchar](64) NOT NULL,
	[CreateTime] [datetimeoffset](7) NOT NULL,
	[UpdatedTime] [datetimeoffset](7) NOT NULL,
	[LastAggregateSequenceNumber] [int] NOT NULL,
	[LastUpgradedId] [nvarchar](64) NULL,
	CONSTRAINT [PK_ReadModel-ThingyAggregate] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-ThingyAggregate_AggregateId] ON [dbo].[ReadModel-ThingyAggregate]
(
	[AggregateId] ASC
)
