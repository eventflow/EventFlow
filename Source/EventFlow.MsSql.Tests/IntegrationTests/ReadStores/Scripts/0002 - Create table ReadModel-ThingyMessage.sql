CREATE TABLE [$MsSqlSchema$].[ReadModel-ThingyMessage](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ThingyId] [nvarchar](64) NOT NULL,
	[MessageId] [nvarchar](64) NOT NULL,
	[Message] [nvarchar](MAX) NOT NULL,
	CONSTRAINT [PK_ReadModel-ThingyMessage] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-ThingyMessage_AggregateId] ON [$MsSqlSchema$].[ReadModel-ThingyMessage]
(
	[MessageId] ASC
)

CREATE NONCLUSTERED INDEX [IX_ReadModel-ThingyMessage_ThingyId] ON [$MsSqlSchema$].[ReadModel-ThingyMessage]
(
	[ThingyId] ASC
)
