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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.Cancellation;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.Tests.IntegrationTests.ReadStores.ReadModels;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class CancellationTests
    {
        private ICommandBus _commandBus;
        private ManualCommandHandler _commandHandler;
        private ManualEventPersistence _eventPersistence;
        private ManualReadStore _readStore;
        private ManualSubscriber _subscriber;

        [TestCaseSource(nameof(GetTestCases))]
        public async Task ShouldCancelBeforeBarrierOrRunToEnd(
            CancellationBoundary configuredBoundary,
            CancellationBoundary cancelAt)
        {
            // Arrange

            Configure(configuredBoundary);

            var safetyTimeout = Debugger.IsAttached 
                ? TimeSpan.FromDays(1) 
                : TimeSpan.FromSeconds(1);

            var id = ThingyId.New;
            var pingId = PingId.New;
            var tokenSource = new CancellationTokenSource(safetyTimeout);
            var token = tokenSource.Token;

            var steps = CreateSteps(id);

            // Act

            var publishTask = _commandBus.PublishAsync(new ThingyPingCommand(id, pingId), token);

            RunUpTo(steps, cancelAt);
            tokenSource.Cancel();
            RunAfter(steps, cancelAt);

            var publishTaskOrSafetyTimeout = await Task.WhenAny(
                publishTask,
                Task.Delay(safetyTimeout, CancellationToken.None));

            if (publishTaskOrSafetyTimeout == publishTask)
            {
                try
                {
                    // Command could have failed or been cancelled.
                    await publishTask;
                }
                catch (OperationCanceledException)
                {
                    // Command was cancelled.
                }
            }
            else
            {
                throw new Exception("Test timeout: Cancellation didn't work.");
            }

            // Assert

            var shouldHaveRunTo = cancelAt <= configuredBoundary
                ? cancelAt
                : CancellationBoundary.CancelAlways; // Run to end

            await Validate(steps, shouldHaveRunTo);
        }

        private static IEnumerable<TestCaseData> GetTestCases()
        {
            return
                from configuredBoundary in GetBoundaries()
                from cancelAt in GetBoundaries()
                select new TestCaseData(configuredBoundary, cancelAt);
        }

        private List<IStep> CreateSteps(ThingyId id)
        {
            var steps = new List<IStep>
            {
                new Step<bool>(
                    CancellationBoundary.BeforeUpdatingAggregate,
                    _eventPersistence.LoadCompletionSource),

                new Step<bool>(
                    CancellationBoundary.BeforeCommittingEvents,
                    _commandHandler.ExecuteCompletionSource,
                    () => Task.FromResult(_commandHandler.HasBeenCalled),
                    v => v.Should().BeTrue(),
                    v => v.Should().BeFalse()),

                new Step<IReadOnlyCollection<ICommittedDomainEvent>>(
                    CancellationBoundary.BeforeUpdatingReadStores,
                    _eventPersistence.CommitCompletionSource,
                    () => _eventPersistence.LoadCommittedEventsAsync(id, 0, CancellationToken.None),
                    v => v.Should().NotBeEmpty(),
                    v => v.Should().BeEmpty()),

                new Step<ReadModelEnvelope<InMemoryThingyReadModel>>(
                    CancellationBoundary.BeforeNotifyingSubscribers,
                    _readStore.UpdateCompletionSource,
                    () => _readStore.GetAsync(id.ToString(), CancellationToken.None),
                    v => v.ReadModel.Should().NotBeNull(),
                    v => v.ReadModel.Should().BeNull()),

                new Step<bool>(
                    CancellationBoundary.CancelAlways,
                    _subscriber.HandleCompletionSource,
                    () => Task.FromResult(_subscriber.HasHandled),
                    v => v.Should().BeTrue(),
                    v => v.Should().BeFalse())
            };

            return steps;
        }

        private static IEnumerable<CancellationBoundary> GetBoundaries()
        {
            return Enum.GetValues(typeof(CancellationBoundary))
                .Cast<CancellationBoundary>()
                .OrderBy(b => b);
        }

        private void Configure(CancellationBoundary testBoundary)
        {
            _commandHandler = new ManualCommandHandler();
            _subscriber = new ManualSubscriber();
            _eventPersistence = null;
            _readStore = null;

            var resolver = EventFlowOptions
                .New
                .AddCommands(typeof(ThingyPingCommand))
                .AddEvents(typeof(ThingyPingEvent))
                .UseInMemoryReadStoreFor<InMemoryThingyReadModel>()
                .Configure(c => c.CancellationBoundary = testBoundary)
                .RegisterServices(s =>
                {
                    s.Decorate<IInMemoryReadStore<InMemoryThingyReadModel>>((c, i) =>
                        _readStore ?? (_readStore = new ManualReadStore(i)));
                    s.Decorate<IEventPersistence>((c, i) =>
                        _eventPersistence ?? (_eventPersistence = new ManualEventPersistence(i)));
                    s.Register<ICommandHandler<ThingyAggregate, ThingyId, IExecutionResult, ThingyPingCommand>>(c =>
                        _commandHandler);
                    s.Register<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>>(c => _subscriber);
                    s.Register<IScopedContext, ScopedContext>(Lifetime.Scoped);
                })
                .CreateResolver();

            _commandBus = resolver.Resolve<ICommandBus>();
        }

        private static async Task Validate(IEnumerable<IStep> steps, CancellationBoundary shouldHaveRunTo)
        {
            foreach (var step in steps)
            {
                if (step.Boundary <= shouldHaveRunTo)
                    await step.ValidateHasRunAsync();
                else
                    await step.ValidateHasNotRunAsync();
            }
        }

        private static void RunUpTo(IEnumerable<IStep> steps, CancellationBoundary boundary)
        {
            foreach (var step in steps.Where(s => s.Boundary < boundary))
            {
                step.Trigger();
            }
        }

        private static void RunAfter(IEnumerable<IStep> steps, CancellationBoundary boundary)
        {
            foreach (var step in steps.Where(s => s.Boundary >= boundary))
            {
                step.Trigger();
            }
        }

        private interface IStep
        {
            CancellationBoundary Boundary { get; }
            void Trigger();
            Task ValidateHasRunAsync();
            Task ValidateHasNotRunAsync();
        }

        private class Step<T> : IStep
        {
            private readonly TaskCompletionSource<bool> _completionSource;
            private readonly Action<T> _validateHasNotRun;
            private readonly Action<T> _validateHasRun;
            private readonly Func<Task<T>> _validationFactory;

            public Step(
                CancellationBoundary boundary,
                TaskCompletionSource<bool> completionSource,
                Func<Task<T>> validationFactory = null,
                Action<T> validateHasRun = null,
                Action<T> validateHasNotRun = null)
            {
                Boundary = boundary;
                _completionSource = completionSource;
                _validationFactory = validationFactory ?? (() => Task.FromResult(default(T)));
                _validateHasRun = validateHasRun ?? (_ => { });
                _validateHasNotRun = validateHasNotRun ?? (_ => { });
            }

            public CancellationBoundary Boundary { get; }

            public void Trigger()
            {
                _completionSource?.SetResult(true);
            }

            public async Task ValidateHasRunAsync()
            {
                var value = await _validationFactory();
                _validateHasRun(value);
            }

            public async Task ValidateHasNotRunAsync()
            {
                var value = await _validationFactory();
                _validateHasNotRun(value);
            }
        }

        private class ManualCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyPingCommand>
        {
            public TaskCompletionSource<bool> ExecuteCompletionSource { get; } = new TaskCompletionSource<bool>();

            public bool HasBeenCalled { get; private set; }

            public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyPingCommand command,
                CancellationToken cancellationToken)
            {
                HasBeenCalled = true;
                aggregate.Ping(command.PingId);
                return ExecuteCompletionSource.Task;
            }
        }

        private class ManualReadStore : IInMemoryReadStore<InMemoryThingyReadModel>
        {
            private readonly IInMemoryReadStore<InMemoryThingyReadModel> _inner;

            public ManualReadStore(IInMemoryReadStore<InMemoryThingyReadModel> inner = null)
            {
                _inner = inner ?? new InMemoryReadStore<InMemoryThingyReadModel>(new ConsoleLog());
            }

            public TaskCompletionSource<bool> UpdateCompletionSource { get; } = new TaskCompletionSource<bool>();

            public Task<IReadOnlyCollection<InMemoryThingyReadModel>> FindAsync(
                Predicate<InMemoryThingyReadModel> predicate, CancellationToken cancellationToken)
            {
                return _inner.FindAsync(predicate, cancellationToken);
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken)
            {
                return _inner.DeleteAsync(id, cancellationToken);
            }

            public Task DeleteAllAsync(CancellationToken cancellationToken)
            {
                return _inner.DeleteAllAsync(cancellationToken);
            }

            public Task<ReadModelEnvelope<InMemoryThingyReadModel>> GetAsync(string id,
                CancellationToken cancellationToken)
            {
                return _inner.GetAsync(id, cancellationToken);
            }

            public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates, IReadModelContextFactory readModelContextFactory,
                Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<InMemoryThingyReadModel>, CancellationToken, Task<ReadModelUpdateResult<InMemoryThingyReadModel>>> updateReadModel, CancellationToken cancellationToken)
            {
                await _inner.UpdateAsync(readModelUpdates, readModelContextFactory, updateReadModel, cancellationToken);
                await UpdateCompletionSource.Task;
            }
        }

        private class ManualSubscriber : ISubscribeSynchronousTo<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public TaskCompletionSource<bool> HandleCompletionSource { get; } = new TaskCompletionSource<bool>();

            public bool HasHandled { get; private set; }

            public async Task HandleAsync(IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
                CancellationToken cancellationToken)
            {
                await HandleCompletionSource.Task;
                HasHandled = true;
            }
        }

        private class ManualEventPersistence : IEventPersistence
        {
            private readonly IEventPersistence _inner;

            public ManualEventPersistence(IEventPersistence inner)
            {
                _inner = inner;
            }

            public TaskCompletionSource<bool> CommitCompletionSource { get; } = new TaskCompletionSource<bool>();
            public TaskCompletionSource<bool> LoadCompletionSource { get; } = new TaskCompletionSource<bool>();

            public Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize,
                CancellationToken cancellationToken)
            {
                return _inner.LoadAllCommittedEvents(globalPosition, pageSize, cancellationToken);
            }

            public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id,
                IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
            {
                var result = await _inner.CommitEventsAsync(id, serializedEvents, cancellationToken);
                await CommitCompletionSource.Task;
                return result;
            }

            public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id,
                int fromEventSequenceNumber, CancellationToken cancellationToken)
            {
                var result = await _inner.LoadCommittedEventsAsync(id, fromEventSequenceNumber, cancellationToken);
                await LoadCompletionSource.Task;
                return result;
            }

            public Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
            {
                return _inner.DeleteEventsAsync(id, cancellationToken);
            }
        }
    }
}
