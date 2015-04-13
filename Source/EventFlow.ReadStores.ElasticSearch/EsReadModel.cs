using System;

namespace EventFlow.ReadStores.ElasticSearch
{
    public class EsReadModel : IEsReadModel
    {
        public string AggregateId { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset UpdatedTime { get; set; }
        public int LastAggregateSequenceNumber { get; set; }
        public long LastGlobalSequenceNumber { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Read model '{0}' for '{1} ({2}/{3}'",
                GetType().Name,
                AggregateId,
                LastGlobalSequenceNumber,
                LastAggregateSequenceNumber);
        }
    }
}