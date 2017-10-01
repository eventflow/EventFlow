// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
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
        
        public class TestResultCommand : Command<ThingyAggregate, ThingyId, TestExecutionResult>
        {
            public TestResultCommand(ThingyId aggregateId) : base(aggregateId, Core.SourceId.New)
            {
            }
        }

        public class TestResultCommandHandler : CommandHandler<ThingyAggregate, ThingyId, TestExecutionResult, TestResultCommand>
        {
            public override Task<TestExecutionResult> ExecuteCommandAsync(ThingyAggregate aggregate, TestResultCommand command, CancellationToken cancellationToken)
            {
                return Task.FromResult(new TestExecutionResult(42, true));
            }
        }

        [Test]
        public async Task CommandResult()
        {
            using (var resolver = EventFlowOptions.New
                .AddDefaults(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(typeof(TestResultCommandHandler))
                .CreateResolver(false))
            {
                var commandBus = resolver.Resolve<ICommandBus>();

                var result = await commandBus.PublishAsync(
                    new TestResultCommand(ThingyId.New),
                    CancellationToken.None)
                    .ConfigureAwait(false);

                result.IsSuccess.Should().BeTrue();
                result.MagicNumber.Should().Be(42);
            }
        }
    }
}