﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class CommandBusTests : TestsFor<CommandBus>
    {
        private Mock<IAggregateStore> _aggregateStoreMock;
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            Inject<IMemoryCache>(new DictionaryMemoryCache(Mock<ILog>()));

            _resolverMock = InjectMock<IResolver>();
            _aggregateStoreMock = InjectMock<IAggregateStore>();
        }

        [Test]
        public async Task CommandHandlerIsInvoked()
        {
            // Arrange
            ArrangeWorkingEventStore();
            var commandHandler = ArrangeCommandHandlerExists<ThingyAggregate, ThingyId, ISourceId, ThingyPingCommand>();

            // Act
            await Sut.PublishAsync(new ThingyPingCommand(ThingyId.New, PingId.New)).ConfigureAwait(false);

            // Assert
            commandHandler.Verify(h => h.ExecuteAsync(It.IsAny<ThingyAggregate>(), It.IsAny<ThingyPingCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private void ArrangeWorkingEventStore()
        {
            _aggregateStoreMock
                .Setup(s => s.UpdateAsync(It.IsAny<ThingyId>(), It.IsAny<ISourceId>(), It.IsAny<Func<ThingyAggregate, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Callback<ThingyId, ISourceId, Func<ThingyAggregate, CancellationToken, Task>, CancellationToken>((i, s, f, c) => f(A<ThingyAggregate>(), c))
                .Returns(() => Task.FromResult<IReadOnlyCollection<IDomainEvent>>(Many<IDomainEvent<ThingyAggregate, ThingyId>>()));
        }

        private void ArrangeCommandHandlerExists<TAggregate, TIdentity, TSourceIdentity, TCommand>(
            ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand> commandHandler)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
            where TCommand : ICommand<TAggregate, TIdentity, TSourceIdentity>
        {
            _resolverMock
                .Setup(r => r.ResolveAll(typeof(ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>)))
                .Returns(new[] { commandHandler });
        }

        private Mock<ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>> ArrangeCommandHandlerExists<TAggregate, TIdentity, TSourceIdentity, TCommand>()
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TSourceIdentity : ISourceId
            where TCommand : ICommand<TAggregate, TIdentity, TSourceIdentity>
        {
            var mock = new Mock<ICommandHandler<TAggregate, TIdentity, TSourceIdentity, TCommand>>();
            ArrangeCommandHandlerExists(mock.Object);
            return mock;
        }
    }
}