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
using EventFlow.Extensions;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.Exploration
{
    [Category(Categories.Integration)]
    public class RegisterSubscribersExplorationTests : Test
    {
        [TestCaseSource(nameof(TestCases))]
        public async Task TestRegisterAsynchronousSubscribersAsync(Func<IEventFlowOptions, IEventFlowOptions> register)
        {
            // Arrange
            var wasHandled = false;
            TestSubscriber.OnHandleAction = () => wasHandled = true;
            using (new DisposableAction(() => TestSubscriber.OnHandleAction = null))
            using (var resolver =  register(EventFlowOptions.New)
                .AddCommands(typeof(ThingyPingCommand))
                .AddCommandHandlers(typeof(ThingyPingCommandHandler))
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .AddEvents(typeof(ThingyPingEvent))
                .Configure(c => c.IsAsynchronousSubscribersEnabled = true)
                .CreateResolver(false))
            {
                var commandBus = resolver.Resolve<ICommandBus>();

                // Act
                await commandBus.PublishAsync(
                    new ThingyPingCommand(A<ThingyId>(), A<PingId>()),
                    CancellationToken.None)
                    .ConfigureAwait(false);
            }

            // Assert
            wasHandled.Should().BeTrue();
        }

        public static IEnumerable<Func<IEventFlowOptions, IEventFlowOptions>> TestCases()
        {
            yield return o =>
                {
                    Console.WriteLine("Using generic");
                    return o.AddAsynchronousSubscriber<ThingyAggregate, ThingyId, ThingyPingEvent, TestSubscriber>() ;
                };
            yield return o =>
                {
                    Console.WriteLine("Using add specific type");
                    return o.AddSubscribers(typeof(TestSubscriber));
                };
            yield return o =>
                {
                    Console.WriteLine("Using assembly scanner");
                    return o.AddSubscribers(typeof(RegisterSubscribersExplorationTests).Assembly, t => t == typeof(TestSubscriber));
                };
        }
    }

    public class TestSubscriber : ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>
    {
        public static Action OnHandleAction { get; set; }

        public Task HandleAsync(IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent, CancellationToken cancellationToken)
        {
            OnHandleAction?.Invoke();
            return Task.FromResult(0);
        }
    }
}