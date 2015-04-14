using System;

namespace EventFlow.ReadStores.Elasticsearch
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