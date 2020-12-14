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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.Tests.UnitTests.Specifications;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class CommandResultTests
    {
        public class TestExecutionResult : ExecutionResult
        {
            public TestExecutionResult(
                int magicNumber,
                bool isSuccess)
            {
                MagicNumber = magicNumber;
                IsSuccess = isSuccess;
            }

            public int MagicNumber { get; }
            public override bool IsSuccess { get; }
        }
        
        public class TestSuccessResultCommand : Command<ThingyAggregate, ThingyId, TestExecutionResult>
        {
            public TestSuccessResultCommand(ThingyId aggregateId) : base(aggregateId, EventFlow.Core.SourceId.New)
            {
            }
        }

        public class TestSuccessResultCommandHandler : CommandHandler<ThingyAggregate, ThingyId, TestExecutionResult, TestSuccessResultCommand>
        {
            public override Task<TestExecutionResult> ExecuteCommandAsync(
                ThingyAggregate aggregate,
                TestSuccessResultCommand command,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new TestExecutionResult(42, true));
            }
        }
        
        
        public class TestFailedResultCommand : Command<ThingyAggregate, ThingyId, IExecutionResult>
        {
            public TestFailedResultCommand(ThingyId aggregateId) : base(aggregateId, EventFlow.Core.SourceId.New)
            {
            }
        }

        public class TestFailedResultCommandHandler : CommandHandler<ThingyAggregate, ThingyId, IExecutionResult, TestFailedResultCommand>
        {
            public override Task<IExecutionResult> ExecuteCommandAsync(
                ThingyAggregate aggregate,
                TestFailedResultCommand command,
                CancellationToken cancellationToken)
            {
                var specification = new TestSpecifications.IsTrueSpecification();
                return Task.FromResult(specification.IsNotSatisfiedByAsExecutionResult(false));
            }
        }

        [Test]
        public async Task CommandResult()
        {
            using (var resolver = EventFlowOptions.New
                .AddCommandHandlers(
                    typeof(TestSuccessResultCommandHandler),
                    typeof(TestFailedResultCommandHandler))
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .CreateResolver(false))
            {
                var commandBus = resolver.Resolve<ICommandBus>();

                var success = await commandBus.PublishAsync(
                    new TestSuccessResultCommand(ThingyId.New),
                    CancellationToken.None)
                    .ConfigureAwait(false);
                success.IsSuccess.Should().BeTrue();
                success.MagicNumber.Should().Be(42);
                
                var failed = await commandBus.PublishAsync(
                        new TestFailedResultCommand(ThingyId.New),
                        CancellationToken.None)
                    .ConfigureAwait(false);
                failed.IsSuccess.Should().BeFalse();
            }
        }
    }
}