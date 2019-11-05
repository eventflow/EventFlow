using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace EventFlow.EventStores.StreamsDb.ReadStores
{
    public class NullReadModelLocator : IReadModelLocator
    {
        public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
        {
            // null doesn't work with the ReadModelEnvelope, so "null" instead
            yield return "null";
        }
    }
}