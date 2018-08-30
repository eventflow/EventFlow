DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'eventdatamodel_list_type') THEN
		CREATE TYPE "eventdatamodel_list_type" AS
		(
			AggregateId varchar(255),
			AggregateName varchar(255),
			AggregateSequenceNumber int,
			BatchId uuid,
			Data TEXT,
			Metadata TEXT
		);
    END IF;
END
$$;