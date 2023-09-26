// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Sagas;
using EventFlow.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Tests.IntegrationTests.Sagas
{
    public class AlternativeSagaStoreTestClasses
    {
        public class TestSagaId : SingleValueObject<string>, ISagaId
        {
            public TestSagaId(string value) : base(value)
            {
            }
        }

        public class InMemorySagaStore : SagaStore
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly Dictionary<ISagaId, object> _sagas = new Dictionary<ISagaId, object>();
            private readonly AsyncLock _asyncLock = new AsyncLock();
            private bool _hasUpdateBeenCalled;

            public InMemorySagaStore(
                IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public void UpdateShouldNotHaveBeenCalled()
            {
                this._hasUpdateBeenCalled.Should().BeFalse();
            }

            public override async Task<ISaga> UpdateAsync(
                ISagaId sagaId,
                Type sagaType,
                ISourceId sourceId,
                Func<ISaga, CancellationToken, Task> updateSaga,
                CancellationToken cancellationToken)
            {
                var commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
                var scheduler = _serviceProvider.GetRequiredService<ICommandScheduler>();

                ISaga saga;
                using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    _hasUpdateBeenCalled = true;

                    if (!_sagas.TryGetValue(sagaId, out var obj))
                    {
                        obj = Activator.CreateInstance(sagaType, sagaId);
                        _sagas[sagaId] = obj;
                    }

                    saga = (ISaga) obj;

                    await updateSaga(saga, cancellationToken).ConfigureAwait(false);
                }
                
                await saga.PublishAsync(commandBus, cancellationToken).ConfigureAwait(false);
                await saga.SchedulePublishAsync(scheduler, cancellationToken).ConfigureAwait(false);

                return saga;
            }
        }

        public class TestSaga : ISaga<TestSagaLocator>,
            ISagaIsStartedBy<SagaTestAggregate, SagaTestAggregateId, SagaTestEventA>,
            ISagaHandles<SagaTestAggregate, SagaTestAggregateId, SagaTestEventB>,
            ISagaHandles<SagaTestAggregate, SagaTestAggregateId, SagaTestEventC>,
            ISagaHandles<SagaTestAggregate, SagaTestAggregateId, SagaTestEventD>
        {
            private readonly ICollection<Func<ICommandBus, CancellationToken, Task>> _unpublishedCommands = new List<Func<ICommandBus, CancellationToken, Task>>();
            private readonly ICollection<Func<ICommandScheduler, CancellationToken, Task>> _scheduledCommands = new List<Func<ICommandScheduler, CancellationToken, Task>>();

            public TestSagaId Id { get; }
            public SagaState State { get; private set; }

            public TestSaga(TestSagaId id)
            {
                Id = id;
                State = SagaState.New;
            }

            public Task HandleAsync(
                IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventA> domainEvent,
                ISagaContext sagaContext,
                CancellationToken cancellationToken)
            {
                Publish(new SagaTestBCommand(domainEvent.AggregateIdentity));
                State = SagaState.Running;
                return Task.FromResult(0);
            }

            public Task HandleAsync(
                IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventB> domainEvent,
                ISagaContext sagaContext,
                CancellationToken cancellationToken)
            {
                Publish(new SagaTestCCommand(domainEvent.AggregateIdentity));
                return Task.FromResult(0);
            }

            public Task HandleAsync(
                IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventC> domainEvent,
                ISagaContext sagaContext,
                CancellationToken cancellationToken)
            {
                _scheduledCommands.Add((b, c) => b.ScheduleAsync(new SagaTestDCommand(domainEvent.AggregateIdentity), DateTimeOffset.Now, c));
                return Task.FromResult(0);
            }

            public Task HandleAsync(
                IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventD> domainEvent, 
                ISagaContext sagaContext, 
                CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public async Task PublishAsync(ICommandBus commandBus, CancellationToken cancellationToken)
            {
                foreach (var unpublishedCommand in _unpublishedCommands.ToList())
                {
                    _unpublishedCommands.Remove(unpublishedCommand);
                    await unpublishedCommand(commandBus, cancellationToken).ConfigureAwait(false);
                }
            }

            public async Task SchedulePublishAsync(ICommandScheduler scheduler, CancellationToken cancellationToken)
            {
                foreach (var unpublishedCommand in _scheduledCommands.ToList())
                {
                    _scheduledCommands.Remove(unpublishedCommand);
                    await unpublishedCommand(scheduler, cancellationToken).ConfigureAwait(false);
                }
            }

            protected void Publish<TCommandAggregate, TCommandAggregateIdentity, TExecutionResult>(
                ICommand<TCommandAggregate, TCommandAggregateIdentity, TExecutionResult> command)
                where TCommandAggregate : IAggregateRoot<TCommandAggregateIdentity>
                where TCommandAggregateIdentity : IIdentity
                where TExecutionResult : IExecutionResult
            {
                _unpublishedCommands.Add((b, c) => b.PublishAsync(command, c));
            }
        }

        public class TestSagaLocator : ISagaLocator
        {
            public Task<ISagaId> LocateSagaAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
            {
                var identity = domainEvent.GetIdentity().Value;
                if (identity.EndsWith(Guid.Empty.ToString()))
                {
                    return Task.FromResult<ISagaId>(null);
                }

                return Task.FromResult<ISagaId>(new TestSagaId($"saga-for-{identity}"));
            }
        }

        public class SagaTestAggregateId : Identity<SagaTestAggregateId>
        {
            public SagaTestAggregateId(string value) : base(value)
            {
            }
        }

        public class SagaTestAggregate : AggregateRoot<SagaTestAggregate, SagaTestAggregateId>
        {
            public int As { get; private set; }
            public int Bs { get; private set; }
            public int Cs { get; private set; }
            public int Ds { get; private set; }

            public SagaTestAggregate(SagaTestAggregateId id) : base(id)
            {
            }

            public void A() { Emit(new SagaTestEventA()); }
            public void B() { Emit(new SagaTestEventB()); }
            public void C() { Emit(new SagaTestEventC()); }
            public void D() { Emit(new SagaTestEventD()); }

            public void Apply(SagaTestEventA e) { As++; }
            public void Apply(SagaTestEventB e) { Bs++; }
            public void Apply(SagaTestEventC e) { Cs++; }
            public void Apply(SagaTestEventD e) { Ds++; }
        }

        public class SagaTestACommand : Command<SagaTestAggregate, SagaTestAggregateId>
        {
            public SagaTestACommand(SagaTestAggregateId aggregateId) : base(aggregateId) { }
        }

        public class SagaTestACommandHandler : CommandHandler<SagaTestAggregate, SagaTestAggregateId, SagaTestACommand>
        {
            public override Task ExecuteAsync(SagaTestAggregate aggregate, SagaTestACommand command, CancellationToken cancellationToken)
            {
                aggregate.A();
                return Task.FromResult(0);
            }
        }

        public class SagaTestBCommand : Command<SagaTestAggregate, SagaTestAggregateId>
        {
            public SagaTestBCommand(SagaTestAggregateId aggregateId) : base(aggregateId) { }
        }

        public class SagaTestBCommandHandler : CommandHandler<SagaTestAggregate, SagaTestAggregateId, SagaTestBCommand>
        {
            public override Task ExecuteAsync(SagaTestAggregate aggregate, SagaTestBCommand command, CancellationToken cancellationToken)
            {
                aggregate.B();
                return Task.FromResult(0);
            }
        }

        public class SagaTestCCommand : Command<SagaTestAggregate, SagaTestAggregateId>
        {
            public SagaTestCCommand(SagaTestAggregateId aggregateId) : base(aggregateId) { }
        }

        public class SagaTestCCommandHandler : CommandHandler<SagaTestAggregate, SagaTestAggregateId, SagaTestCCommand>
        {
            public override Task ExecuteAsync(SagaTestAggregate aggregate, SagaTestCCommand command, CancellationToken cancellationToken)
            {
                aggregate.C();
                return Task.FromResult(0);
            }
        }

        public class SagaTestDCommand : Command<SagaTestAggregate, SagaTestAggregateId>
        {
            public SagaTestDCommand(SagaTestAggregateId aggregateId) : base(aggregateId) { }
        }

        public class SagaTestDCommandHandler : CommandHandler<SagaTestAggregate, SagaTestAggregateId, SagaTestDCommand>
        {
            public override Task ExecuteAsync(SagaTestAggregate aggregate, SagaTestDCommand command, CancellationToken cancellationToken)
            {
                aggregate.D();
                return Task.FromResult(0);
            }
        }

        public class SagaTestEventA : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
        public class SagaTestEventB : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
        public class SagaTestEventC : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
        public class SagaTestEventD : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
    }
}