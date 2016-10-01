CREATE TABLE [ReadModel-ThingyAggregate](
	[Id] [INTEGER] PRIMARY KEY ASC,
    [AggregateId] [nvarchar](64) NOT NULL,
    [Version] INTEGER,
	[PingsReceived] [int] NOT NULL,
	[DomainErrorAfterFirstReceived] [bit] NOT NULL
)