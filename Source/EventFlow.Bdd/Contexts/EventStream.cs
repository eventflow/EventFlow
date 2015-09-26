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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Bdd.Contexts
{
    public class EventStream : IEventStream
    {
        private readonly ILog _log;
        private readonly ConcurrentDictionary<Guid, IObserver<IDomainEvent>> _observers = new ConcurrentDictionary<Guid, IObserver<IDomainEvent>>();

        public EventStream(
            ILog log)
        {
            _log = log;
        }

        public IDisposable Subscribe(IObserver<IDomainEvent> observer)
        {
            var id = Guid.NewGuid();
            _observers.AddOrUpdate(id, observer, (g, o) => o);
            return new DisposableAction(() =>
            {
                IObserver<IDomainEvent> o;
                _observers.TryRemove(id, out o);
            });
        }

        public Task HandleAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            foreach (var observer in _observers.Values)
            {
                try
                {
                    foreach (var domainEvent in domainEvents)
                    {
                        observer.OnNext(domainEvent);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e, $"Observer '{observer.GetType().PrettyPrint()}' failed!");
                }
            }

            return Task.FromResult(0);
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _disposeAction;

            public DisposableAction(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction();
            }
        }
    }
}