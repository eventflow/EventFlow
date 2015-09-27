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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Bdd.Contexts;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Bdd.Steps
{
    public class ValidateEventHappendScenarioStep<TAggregate, TIdentity, TAggregateEvent> : IScenarioStep, IObserver<IDomainEvent>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TAggregateEvent : IAggregateEvent<TAggregate, TIdentity>
    {
        private readonly IScenarioContext _scenarioContext;
        private readonly TIdentity _identity;
        private readonly Predicate<IDomainEvent<TAggregate, TIdentity, TAggregateEvent>> _predicate;
        private readonly IDisposable _eventStreamSubscription;
        private readonly EventDefinition _eventDescription;
        private bool _gotEvent;

        public string Name { get; }

        public ValidateEventHappendScenarioStep(
            IScenarioContext scenarioContext,
            IResolver resolver,
            TIdentity identity,
            Predicate<IDomainEvent<TAggregate, TIdentity, TAggregateEvent>> predicate)
        {
            _scenarioContext = scenarioContext;
            _identity = identity;
            _predicate = predicate;
            _eventStreamSubscription = resolver.Resolve<IEventStream>().Subscribe(this);
            _eventDescription = resolver.Resolve<IEventDefinitionService>().GetEventDefinition(typeof (TAggregateEvent));

            Name = $"{_eventDescription.Name} v{_eventDescription.Version} happend";
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!_gotEvent)
            {
                throw new Exception($"Did not get '{_eventDescription.Name}' v{_eventDescription.Version}");
            }

            return Task.FromResult(0);
        }

        public void OnNext(IDomainEvent value)
        {
            if (_scenarioContext.Script.State != ScenarioState.When)
            {
                return;
            }

            if (value.GetIdentity().Value != _identity.Value)
            {
                return;
            }

            var domainEvent = value as IDomainEvent<TAggregate, TIdentity, TAggregateEvent>;
            if (domainEvent == null)
            {
                return;
            }

            if (!_predicate(domainEvent))
            {
                return;
            }

            _gotEvent = true;
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public void Dispose()
        {
            _eventStreamSubscription.Dispose();
        }
    }
}
