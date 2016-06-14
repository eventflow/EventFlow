IF NOT EXISTS (SELECT * FROM SYS.TYPES WHERE is_table_type = 1 AND name = 'eventdatamodel_list_type')
BEGIN
	CREATE TYPE eventdatamodel_list_type AS TABLE
	(
		[AggregateId] [nvarchar](255) NOT NULL,
		[AggregateName] [nvarchar](255) NOT NULL,
		[AggregateSequenceNumber] [int] NOT NULL,
		[BatchId] [uniqueidentifier] NOT NULL,
		[Data] [nvarchar](max) NOT NULL,
		[Metadata] [nvarchar](max) NOT NULL
	)
END
