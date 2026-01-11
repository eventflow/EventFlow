using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using System;

namespace EventFlow.MsSql.Tests.IntegrationTests.ReadStores.ReadModels
{
    public interface IThingyReadModel : IReadModel
    {
        int LastAggregateSequenceNumber { get; }
        public DateTimeOffset CreateTime { get; set; }
        public DateTimeOffset UpdatedTime { get; set; }

        Thingy ToThingy();
    }
}
