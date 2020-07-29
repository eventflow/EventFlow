// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.EventStores
{
    public class DomainEventFactory : IDomainEventFactory
    {
        private static readonly ConcurrentDictionary<Type, Type> AggregateEventToDomainEventTypeMap = new ConcurrentDictionary<Type, Type>();
        private static readonly ConcurrentDictionary<Type, Type> DomainEventToIdentityTypeMap = new ConcurrentDictionary<Type, Type>();

        public IDomainEvent Create(
            IAggregateEvent aggregateEvent,
            IMetadata metadata,
            string aggregateIdentity,
            int aggregateSequenceNumber)
        {
            var domainEventType = AggregateEventToDomainEventTypeMap.GetOrAdd(aggregateEvent.GetType(), GetDomainEventType);
            var identityType = DomainEventToIdentityTypeMap.GetOrAdd(domainEventType, GetIdentityType);
            var identity = Activator.CreateInstance(identityType, aggregateIdentity);

            var domainEvent = (IDomainEvent)Activator.CreateInstance(
                domainEventType,
                aggregateEvent,
                metadata,
                metadata.Timestamp,
                identity,
                aggregateSequenceNumber);

            return domainEvent;
        }

        public IDomainEvent<TAggregate, TIdentity> Create<TAggregate, TIdentity>(
            IAggregateEvent aggregateEvent,
            IMetadata metadata,
            TIdentity id,
            int aggregateSequenceNumber)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return (IDomainEvent<TAggregate, TIdentity>)Create(
                aggregateEvent,
                metadata,
                id.Value,
                aggregateSequenceNumber);
        }

        public IDomainEvent<TAggregate, TIdentity> Upgrade<TAggregate, TIdentity>(
            IDomainEvent domainEvent,
            IAggregateEvent aggregateEvent)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return Create<TAggregate, TIdentity>(
                aggregateEvent,
                domainEvent.Metadata,
                (TIdentity) domainEvent.GetIdentity(),
                domainEvent.AggregateSequenceNumber);
        }

        private static Type GetIdentityType(Type domainEventType)
        {
            var domainEventInterfaceType = domainEventType
                .GetTypeInfo()
                .GetInterfaces()
                .SingleOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEvent<,>));

            if (domainEventInterfaceType == null)
            {
                throw new ArgumentException($"Type '{domainEventType.PrettyPrint()}' is not a '{typeof(IDomainEvent<,>).PrettyPrint()}'");
            }

            var genericArguments = domainEventInterfaceType.GetTypeInfo().GetGenericArguments();
            return genericArguments[1];
        }

        private static Type GetDomainEventType(Type aggregateEventType)
        {
            var aggregateEventInterfaceType = aggregateEventType
                .GetTypeInfo()
                .GetInterfaces()
                .SingleOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateEvent<,>));

            if (aggregateEventInterfaceType == null)
            {
                throw new ArgumentException($"Type '{aggregateEventType.PrettyPrint()}' is not a '{typeof(IAggregateEvent<,>).PrettyPrint()}'");
            }

            var genericArguments = aggregateEventInterfaceType.GetTypeInfo().GetGenericArguments();
            return typeof(DomainEvent<,,>).MakeGenericType(genericArguments[0], genericArguments[1], aggregateEventType);
        }
    }
}
