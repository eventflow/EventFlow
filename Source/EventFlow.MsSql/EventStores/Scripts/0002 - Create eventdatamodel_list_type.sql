IF NOT EXISTS (
	SELECT *
	FROM sys.table_types table_types
	INNER JOIN sys.schemas schemas ON table_types.schema_id = schemas.schema_id
	WHERE
		table_types.name = N'eventdatamodel_list_type' AND
		schemas.name = N'$MsSqlSchema$')
BEGIN
	CREATE TYPE [$MsSqlSchema$].eventdatamodel_list_type AS TABLE
	(
		[AggregateId] [nvarchar](255) NOT NULL,
		[AggregateName] [nvarchar](255) NOT NULL,
		[AggregateSequenceNumber] [int] NOT NULL,
		[BatchId] [uniqueidentifier] NOT NULL,
		[Data] [nvarchar](max) NOT NULL,
		[Metadata] [nvarchar](max) NOT NULL
	)
END
