// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Bdd.Steps
{
    public class EventScenarioStep<TAggregate, TIdentity, TAggregateEvent> : IScenarioStep
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TAggregateEvent : IAggregateEvent<TAggregate, TIdentity>
    {
        private readonly IResolver _resolver;
        private readonly TIdentity _identity;
        private readonly TAggregateEvent _aggregateEvent;

        public string Title { get; }
        public string Description { get; }

        public EventScenarioStep(IResolver resolver, TIdentity identity, TAggregateEvent aggregateEvent)
        {
            _resolver = resolver;
            _identity = identity;
            _aggregateEvent = aggregateEvent;

            var eventDescription = resolver.Resolve<IEventDefinitionService>().GetEventDefinition(aggregateEvent.GetType());

            Title = $"{eventDescription.Name} is emitted";
            Description = Title;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var eventStore = _resolver.Resolve<IEventStore>();

            var aggregateRoot = await eventStore.LoadAggregateAsync<TAggregate, TIdentity>(
                _identity,
                cancellationToken)
                .ConfigureAwait(false);

            aggregateRoot.InjectUncommittedEvents(new IAggregateEvent[] { _aggregateEvent });

            await aggregateRoot.CommitAsync(
                eventStore,
                SourceId.New,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
        }
    }
}