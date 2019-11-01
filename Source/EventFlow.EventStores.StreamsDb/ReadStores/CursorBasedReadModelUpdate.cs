using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.ReadStores;

namespace EventFlow.EventStores.StreamsDb
{
	public class CursorBasedReadModelUpdate : ReadModelUpdate
	{
		public Dictionary<string, long> Cursors { get; }

		public CursorBasedReadModelUpdate(string readModelId, IReadOnlyCollection<IDomainEvent> domainEvents, Dictionary<string, long> cursors) 
			: base(readModelId, domainEvents)
		{
			Cursors = cursors;
		}
	}
}
