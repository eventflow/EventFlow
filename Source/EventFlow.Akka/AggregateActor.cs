// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Akka
{
    public class AggregateActor<TAggregate, TIdentity> : UntypedPersistentActor
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IAggregateFactory _aggregateFactory;

        private TAggregate _aggregate;

        public override string PersistenceId => _aggregate.Id.Value;

        protected override void OnRecover(object message)
        {
            throw new System.NotImplementedException();
        }

        public AggregateActor(
            ILog log,
            IResolver resolver,
            IAggregateFactory aggregateFactory)
        {
            _log = log;
            _resolver = resolver;
            _aggregateFactory = aggregateFactory;
        }

        protected override void OnCommand(object message)
        {
            _log.Verbose(() => $"Actor '{Self.Path}' handling {message.GetType().PrettyPrint()}");
            var command = (ICommand<TAggregate, TIdentity>)message;
            if (_aggregate == null)
            {
                using (var w = AsyncHelper.Wait)
                {
                    w.Run(_aggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(command.AggregateId), a => _aggregate = a);
                }
            }

            var commandType = message.GetType();
            var commandInterface = commandType
                .GetTypeInfo()
                .GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<,,>));

            var genericArguments = commandInterface.GetGenericArguments();
            var commandHandlerType = typeof(ICommandHandler<,,,>)
                .MakeGenericType(
                    genericArguments[0],
                    genericArguments[1],
                    genericArguments[2],
                    commandType);

            var commandHandler = _resolver.Resolve(commandHandlerType);

            using (var w = AsyncHelper.Wait)
            {
                w.Run((Task)commandHandler.GetType().GetMethod("ExecuteCommandAsync").Invoke(
                    commandHandler,
                    new object[]{ _aggregate, command, CancellationToken.None}));
            }

            var eventCount = _aggregate.UncommittedEvents.Count();
            if (eventCount == 0)
            {
                _log.Verbose(() => $"Actor '{Self.Path}' didn't emit events after handling {message.GetType().PrettyPrint()}");
            }

            _log.Verbose(() => $"Actor '{Self.Path}' emitted events {eventCount} after handling {message.GetType().PrettyPrint()}");

        }
    }
}
