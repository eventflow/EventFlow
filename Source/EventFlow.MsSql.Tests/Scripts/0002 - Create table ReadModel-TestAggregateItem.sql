CREATE TABLE [dbo].[ReadModel-TestAggregateItem](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[AggregateId] [nvarchar](64) NOT NULL,
	[CreateTime] [datetimeoffset](7) NOT NULL,
	[UpdatedTime] [datetimeoffset](7) NOT NULL,
	[LastAggregateSequenceNumber] [int] NOT NULL,
	CONSTRAINT [PK_ReadModel-TestAggregateItem] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-TestAggregateItem_AggregateId] ON [dbo].[ReadModel-TestAggregateItem]
(
	[AggregateId] ASC
)
