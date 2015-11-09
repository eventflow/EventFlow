CREATE TABLE [dbo].[ReadModel-ThingyMessage](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ThingyId] [nvarchar](64) NOT NULL,
	[AggregateId] [nvarchar](64) NOT NULL,
	[Message] [nvarchar](MAX) NOT NULL,
	CONSTRAINT [PK_ReadModel-ThingyMessage] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-ThingyMessage_AggregateId] ON [dbo].[ReadModel-ThingyMessage]
(
	[AggregateId] ASC
)

CREATE NONCLUSTERED INDEX [IX_ReadModel-ThingyMessage_ThingyId] ON [dbo].[ReadModel-ThingyMessage]
(
	[ThingyId] ASC
)
