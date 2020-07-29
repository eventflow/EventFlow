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
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;

namespace EventFlow.Extensions
{
    public static class CommandBusExtensions
    {
        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static TExecutionResult Publish<TAggregate, TIdentity, TExecutionResult>(
            this ICommandBus commandBus,
            ICommand<TAggregate, TIdentity, TExecutionResult> command)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            return commandBus.Publish(command, CancellationToken.None);
        }

        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static TExecutionResult Publish<TAggregate, TIdentity, TExecutionResult>(
            this ICommandBus commandBus,
            ICommand<TAggregate, TIdentity, TExecutionResult> command,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var result = default(TExecutionResult);

            using (var a = AsyncHelper.Wait)
            {
                a.Run(commandBus.PublishAsync(command, cancellationToken), id => result = id);
            }

            return result;
        }
    }
}