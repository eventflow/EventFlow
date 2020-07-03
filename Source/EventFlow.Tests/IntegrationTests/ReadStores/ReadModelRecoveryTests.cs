// The MIT License (MIT)
//
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
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
using EventFlow.Extensions;
using EventFlow.PublishRecovery;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.ReadStores
{
    public sealed class ReadModelRecoveryTests : IntegrationTest
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .UseInMemoryReadStoreFor<FailingReadModel>()
                .UseReadModelRecoveryHandler<FailingReadModel, TestRecoveryHandler>(Lifetime.Singleton)
                .CreateResolver();
        }

        [Test]
        public async Task ShouldRecoveryForExceptionInReadModel()
        {
            var recoveryHandler = (TestRecoveryHandler)Resolver.Resolve<IReadModelRecoveryHandler<FailingReadModel>>();
            recoveryHandler.ShouldRecover = true;

            await PublishPingCommandAsync(ThingyId.New);

            recoveryHandler.LastRecoveredEvents.Should()
                .ContainSingle(x => x.GetAggregateEvent() is ThingyPingEvent);
        }

        [Test]
        public async Task ShouldThrowOriginalErrorWhenNoRecovery()
        {
            var recoveryHandler = (TestRecoveryHandler)Resolver.Resolve<IReadModelRecoveryHandler<FailingReadModel>>();
            recoveryHandler.ShouldRecover = false;

            Func<Task> publishPing = () => PublishPingCommandAsync(ThingyId.New);

            (await publishPing.Should().ThrowAsync<Exception>().ConfigureAwait(false))
                .WithMessage("Read model exception. Should be recovered.");
        }

        private sealed class FailingReadModel : IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
            {
                throw new Exception("Read model exception. Should be recovered.");
            }
        }

        private sealed class TestRecoveryHandler : IReadModelRecoveryHandler<FailingReadModel>
        {
            public IReadOnlyCollection<IDomainEvent> LastRecoveredEvents { get; private set; }

            public bool ShouldRecover { get; set; }

            public Task RecoverFromShutdownAsync(IReadOnlyCollection<IDomainEvent> eventsForRecovery, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public Task<bool> RecoverFromErrorAsync(IReadOnlyCollection<IDomainEvent> eventsForRecovery, Exception exception,
                CancellationToken cancellationToken)
            {
                LastRecoveredEvents = eventsForRecovery;

                return Task.FromResult(ShouldRecover);
            }
        }
    }
}