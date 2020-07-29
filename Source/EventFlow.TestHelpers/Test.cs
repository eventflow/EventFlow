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
using System.Collections.Generic;
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Logs;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public abstract class Test
    {
        protected IFixture Fixture { get; private set; }
        protected IDomainEventFactory DomainEventFactory;
        protected ILog Log => LogHelper.Log;

        [SetUp]
        public void SetUpTest()
        {
            Fixture = new Fixture().Customize(new AutoMoqCustomization());

            Fixture.Customize<ThingyId>(x => x.FromFactory(() => ThingyId.New));
            Fixture.Customize<ThingyMessageId>(x => x.FromFactory(() => ThingyMessageId.New));
            Fixture.Customize<EventId>(c => c.FromFactory(() => EventId.New));
            Fixture.Customize<Label>(s => s.FromFactory(() => Label.Named($"label-{Guid.NewGuid():D}")));

            DomainEventFactory = new DomainEventFactory();
        }

        protected T A<T>()
        {
            return Fixture.Create<T>();
        }

        protected List<T> Many<T>(int count = 3)
        {
            return Fixture.CreateMany<T>(count).ToList();
        }

        protected T Mock<T>()
            where T : class
        {
            return new Mock<T>().Object;
        }

        protected T Inject<T>(T instance)
            where T : class
        {
            Fixture.Inject(instance);
            return instance;
        }

        protected Mock<T> InjectMock<T>(params object[] args)
            where T : class
        {
            var mock = new Mock<T>(args);
            Fixture.Inject(mock.Object);
            return mock;
        }

        protected IDomainEvent<ThingyAggregate, ThingyId> ADomainEvent<TAggregateEvent>(int aggregateSequenceNumber = 0)
            where TAggregateEvent : IAggregateEvent
        {
            return ToDomainEvent(A<TAggregateEvent>(), aggregateSequenceNumber);
        }

        protected IReadOnlyCollection<IDomainEvent<ThingyAggregate, ThingyId>> ManyDomainEvents<TAggregateEvent>(
            int count = 3)
            where TAggregateEvent : IAggregateEvent
        {
            return Enumerable.Range(1, count)
                .Select(ADomainEvent<TAggregateEvent>)
                .ToList();
        }

        protected IDomainEvent<ThingyAggregate, ThingyId> ToDomainEvent<TAggregateEvent>(
            TAggregateEvent aggregateEvent,
            int aggregateSequenceNumber = 0)
            where TAggregateEvent : IAggregateEvent
        {
            return ToDomainEvent(A<ThingyId>(), aggregateEvent, aggregateSequenceNumber);
        }

        protected IDomainEvent<ThingyAggregate, ThingyId> ToDomainEvent<TAggregateEvent>(
            ThingyId thingyId,
            TAggregateEvent aggregateEvent,
            int aggregateSequenceNumber = 0)
            where TAggregateEvent : IAggregateEvent
        {
            var metadata = new Metadata
                {
                    Timestamp = A<DateTimeOffset>(),
                    SourceId = A<SourceId>(),
                    EventId = A<EventId>(),
                };

            if (aggregateSequenceNumber == 0)
            {
                aggregateSequenceNumber = A<int>();
            }

            return DomainEventFactory.Create<ThingyAggregate, ThingyId>(
                aggregateEvent,
                metadata,
                thingyId,
                aggregateSequenceNumber);
        }

        protected Mock<Func<T>> CreateFailingFunction<T>(T result, params Exception[] exceptions)
        {
            var function = new Mock<Func<T>>();
            var exceptionStack = new Stack<Exception>(exceptions.Reverse());
            function
                .Setup(f => f())
                .Returns(() =>
                {
                    if (exceptionStack.Any())
                    {
                        throw exceptionStack.Pop();
                    }
                    return result;
                });
            return function;
        }
    }
}