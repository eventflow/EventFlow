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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.Exploration
{
    [Category(Categories.Integration)]
    public class EventUpgradeExplorationTest
    {
        private IResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = EventFlowOptions.New
                .AddEvents(new []{ typeof(UpgradeEventV1), typeof(UpgradeEventV2) })
                .AddEventUpgraders(typeof(UpgradeV1ToV2))
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            (_resolver as IDisposable)?.Dispose();
        }

        [Test]
        public async Task UpgradeEvent()
        {
            // Arrange
            var id = UpgradeId.New;
            var aggregateStore = _resolver.Resolve<IAggregateStore>();

            // Act
            await aggregateStore.UpdateAsync<UpgradeAggregate, UpgradeId>(
                id,
                new SourceId(),
                (a, c) =>
                {
                    a.EmitUpgradeEventV1();
                    return Task.FromResult(0);
                },
                CancellationToken.None);

            // Assert
            var aggregate = await aggregateStore.LoadAsync<UpgradeAggregate, UpgradeId>(
                id,
                CancellationToken.None);
            aggregate.V2Applied.Should().BeTrue();
        }

        public class SourceId : ISourceId
        {
            public string Value { get; } = Guid.NewGuid().ToString("N");
        }

        public class UpgradeId : Identity<UpgradeId>
        {
            public UpgradeId(string value) : base(value) { }
        }

        public class UpgradeAggregate : AggregateRoot<UpgradeAggregate, UpgradeId>,
            IEmit<UpgradeEventV1>,
            IEmit<UpgradeEventV2>
        {
            public UpgradeAggregate(UpgradeId id) : base(id) { }

            public bool V1Applied { get; private set; }
            public bool V2Applied { get; private set; }

            public void EmitUpgradeEventV1()
            {
                Emit(new UpgradeEventV1());
            }

            public void Apply(UpgradeEventV1 aggregateEvent)
            {
                V1Applied = true;
            }

            public void Apply(UpgradeEventV2 aggregateEvent)
            {
                V2Applied = true;
            }
        }

        public class UpgradeEventV1 : IAggregateEvent<UpgradeAggregate, UpgradeId>
        {
        }

        public class UpgradeEventV2 : IAggregateEvent<UpgradeAggregate, UpgradeId>
        {
        }

        public class UpgradeV1ToV2 : IEventUpgrader<UpgradeAggregate, UpgradeId>
        {
            private readonly IDomainEventFactory _domainEventFactory;

            public UpgradeV1ToV2(
                IDomainEventFactory domainEventFactory)
            {
                _domainEventFactory = domainEventFactory;
            }

            public IEnumerable<IDomainEvent<UpgradeAggregate, UpgradeId>> Upgrade(
                IDomainEvent<UpgradeAggregate, UpgradeId> domainEvent)
            {
                var v1 = domainEvent as IDomainEvent<UpgradeAggregate, UpgradeId, UpgradeEventV1>;
                yield return v1 == null
                    ? domainEvent
                    : _domainEventFactory.Upgrade<UpgradeAggregate, UpgradeId>(domainEvent, new UpgradeEventV2());
            }
        }
    }
}
