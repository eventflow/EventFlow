CREATE TABLE [ReadModel-ThingyMessage](
	[Id] [INTEGER] PRIMARY KEY ASC,
	[ThingyId] [nvarchar](64) NOT NULL,
	[Version] INTEGER,
	[MessageId] [nvarchar](64) NOT NULL,
	[Message] [nvarchar](512) NOT NULL
)