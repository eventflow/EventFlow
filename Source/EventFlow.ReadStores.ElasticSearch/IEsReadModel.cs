using System;

namespace EventFlow.ReadStores.ElasticSearch
{
    public interface IEsReadModel : IReadModel
    {
        string AggregateId { get; set; }
        DateTimeOffset CreateTime { get; set; }
        DateTimeOffset UpdatedTime { get; set; }
        int LastAggregateSequenceNumber { get; set; }
        long LastGlobalSequenceNumber { get; set; }
    }
}