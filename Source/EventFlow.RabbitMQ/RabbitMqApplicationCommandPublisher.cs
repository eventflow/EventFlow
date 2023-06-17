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

using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.RabbitMQ.Integrations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.RabbitMQ
{
    public class RabbitMqApplicationCommandPublisher : ICommandBus
    {
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IRabbitMqMessageFactory _rabbitMqMessageFactory;

        public RabbitMqApplicationCommandPublisher(
            IRabbitMqPublisher rabbitMqPublisher,
            IRabbitMqMessageFactory rabbitMqMessageFactory)
        {
            _rabbitMqPublisher = rabbitMqPublisher;
            _rabbitMqMessageFactory = rabbitMqMessageFactory;
        }

        public async Task<TExecutionResult> PublishAsync<TAggregate, TIdentity, TExecutionResult>(ICommand<TAggregate, TIdentity, TExecutionResult> command, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var onlyAcceptsBasicResults = !typeof(TExecutionResult).IsAssignableFrom(typeof(ExecutionResult));
            if (onlyAcceptsBasicResults)
            {
                throw new ArgumentOutOfRangeException(nameof(command), $"Command {command.GetType().Name} expects a non-standard result. RabbitMq only returns success results");
            }

            var rabbitMqMessage = _rabbitMqMessageFactory.CreateMessage(command);

            await _rabbitMqPublisher.PublishAsync(cancellationToken, rabbitMqMessage);

            return (TExecutionResult) ExecutionResult.Success();
        }
    }
}