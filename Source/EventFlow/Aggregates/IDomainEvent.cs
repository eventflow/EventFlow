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
using EventFlow.Core;
using EventFlow.EventSource;

namespace EventFlow.Aggregates
{
    public interface IEntityEvent
    {
        Type EntityType { get; }
        Type EventType { get; }
        IMetadata Metadata { get; }
        DateTimeOffset Timestamp { get; }
        int SequenceNumber { get; }

        IIdentity GetIdentity();

        ISourceEvent GetSourceEvent();
    }

    public interface IDomainEvent : IEntityEvent
    {
        [Obsolete("Use the property 'EntityType' instead")]
        Type AggregateType { get; }

        [Obsolete("Use the property 'SequenceNumber' instead")]
        int AggregateSequenceNumber { get; }

        [Obsolete("Use the method 'GetSourceEvent()' instead")]
        IAggregateEvent GetAggregateEvent();
    }

    public interface IEntityEvent<TEventSourcedEntity, out TIdentity> : IEntityEvent
        where TEventSourcedEntity : IEventSourcedEntity<TIdentity>
        where TIdentity : IIdentity
    {
        TIdentity EntityId { get; }
    }

    public interface IDomainEvent<TAggregate, out TIdentity> : IEntityEvent<TAggregate, TIdentity>, IDomainEvent
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        [Obsolete("Use the property 'EntityId' instead")]
        TIdentity AggregateIdentity { get; }
    }

    public interface IEntityEvent<TEventSourcedEntity, out TIdentity, out TSourceEvent> : IEntityEvent<TEventSourcedEntity, TIdentity>
        where TEventSourcedEntity : IEventSourcedEntity<TIdentity>
        where TIdentity : IIdentity
        where TSourceEvent : ISourceEvent<TEventSourcedEntity, TIdentity>
    {
        TSourceEvent SourceEvent { get; }
    }

    public interface IDomainEvent<TAggregate, out TIdentity, out TSourceEvent> : IEntityEvent<TAggregate, TIdentity, TSourceEvent>, IDomainEvent<TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TSourceEvent : IAggregateEvent<TAggregate, TIdentity>
    {
        [Obsolete("Use the property 'SourceEvent' instead")]
        TSourceEvent AggregateEvent { get; }
    }
}