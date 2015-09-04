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
using System.Collections.Generic;
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.Core;
using EventFlow.EventSourcing;
using EventFlow.EventStores;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;

namespace EventFlow.TestHelpers
{
    public abstract class Test
    {
        protected IFixture Fixture { get; private set; }
        protected IDomainEventFactory DomainEventFactory;

        [SetUp]
        public void SetUpTest()
        {
            Fixture = new Fixture().Customize(new AutoMoqCustomization());
            Fixture.Customize<TestId>(x => x.FromFactory(() => TestId.New));
            Fixture.Customize<Label>(s => s.FromFactory(() => Label.Named(string.Format("label-{0}", Guid.NewGuid().ToString().ToLowerInvariant()))));

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

        protected Mock<T> InjectMock<T>()
            where T : class
        {
            var mock = new Mock<T>();
            Fixture.Inject(mock.Object);
            return mock;
        }

        protected IDomainEvent<TestAggregate, TestId> ToDomainEvent<TAggregateEvent>(
            TAggregateEvent aggregateEvent,
            int aggregateSequenceNumber = 0)
            where TAggregateEvent : IEvent
        {
            var metadata = new Metadata
                {
                    Timestamp = A<DateTimeOffset>(),
                    SourceId = A<SourceId>(),
                };

            if (aggregateSequenceNumber == 0)
            {
                aggregateSequenceNumber = A<int>();
            }

            return DomainEventFactory.Create<TestAggregate, TestId>(
                aggregateEvent,
                metadata,
                A<TestId>(),
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
