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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Sagas;
using EventFlow.ValueObjects;

namespace EventFlow.Tests.UnitTests.Sagas
{
    public class SagaTestClasses
    {
        public class TestSagaId : SingleValueObject<string>, ISagaId
        {
            public TestSagaId(string value) : base(value)
            {
            }
        }

        public class TestSaga : Saga<TestSaga, TestSagaId, TestSagaLocator>,
            ISagaIsStartedBy<SagaTestAggregate, SagaTestAggregateId, SagaTestEventA>,
            ISagaHandles<SagaTestAggregate, SagaTestAggregateId, SagaTestEventB>,
            ISagaHandles<SagaTestAggregate, SagaTestAggregateId, SagaTestEventC>
        {
            private readonly ICommandBus _commandBus;

            public TestSaga(TestSagaId id, ICommandBus commandBus) : base(id)
            {
                _commandBus = commandBus;
            }

            public Task ProcessAsync(IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventA> domainEvent, CancellationToken cancellationToken)
            {
                return _commandBus.PublishAsync(new SagaTestBCommand(domainEvent.AggregateIdentity), cancellationToken);
            }

            public Task ProcessAsync(IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventB> domainEvent, CancellationToken cancellationToken)
            {
                return _commandBus.PublishAsync(new SagaTestCCommand(domainEvent.AggregateIdentity), cancellationToken);
            }

            public Task ProcessAsync(IDomainEvent<SagaTestAggregate, SagaTestAggregateId, SagaTestEventC> domainEvent, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }
        }

        public class TestSagaLocator : ISagaLocator
        {
            public Task<ISagaId> LocateSagaAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
            {
                return Task.FromResult<ISagaId>(new TestSagaId($"saga-for-{domainEvent.GetIdentity().Value}"));
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

            public SagaTestAggregate(SagaTestAggregateId id) : base(id)
            {
            }

            public void A() { Emit(new SagaTestEventA()); }
            public void B() { Emit(new SagaTestEventB()); }
            public void C() { Emit(new SagaTestEventC()); }

            protected void Apply(SagaTestEventA e) { As++; }
            protected void Apply(SagaTestEventB e) { Bs++; }
            protected void Apply(SagaTestEventC e) { Cs++; }
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

        public class SagaTestEventA : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
        public class SagaTestEventB : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
        public class SagaTestEventC : AggregateEvent<SagaTestAggregate, SagaTestAggregateId> { }
    }
}
