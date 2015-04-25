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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.Aggregates
{
    public class AggregateFactory : IAggregateFactory
    {
        private readonly Dictionary<Type, Func<string, object>> _aggregateFactories = new Dictionary<Type, Func<string, object>>();
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly AsyncLock _asyncLock = new AsyncLock();

        public AggregateFactory(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public async Task<TAggregate> CreateNewAggregateAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            using (await _asyncLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                var aggregateType = typeof(TAggregate);
                Func<string, object> aggregateFactory;

                if (!_aggregateFactories.TryGetValue(aggregateType, out aggregateFactory))
                {
                    aggregateFactory = CreateAggregateFactory(aggregateType);
                    _aggregateFactories[aggregateType] = aggregateFactory;
                }

                return (TAggregate) aggregateFactory(id);
            }
        }

        private Func<string, object> CreateAggregateFactory(Type aggregateType)
        {
            var constructors = aggregateType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length > 1)
            {
                throw new ArgumentException(string.Format(
                    "Aggregate type '{0}' has more than one public constructor",
                    aggregateType.Name));
            }
            if (constructors.Length == 0)
            {
                throw new ArgumentException(string.Format(
                    "Aggregate type '{0}' doesn't have any public constructors",
                    aggregateType.Name));
            }
            var constructor = constructors.Single();

            var argumentFactories = constructor
                .GetParameters()
                .Select(p =>
                    {
                        if (p.ParameterType.IsInterface)
                        {
                            return (Func<string, object>)(id => _resolver.Resolve(p.ParameterType));
                        }
                        if (p.ParameterType == typeof (string) && p.Name == "id")
                        {
                            return id => id;
                        }
                        throw new ArgumentException(string.Format(
                            "Aggregate type '{0}' has an argument for its constructor of type '{1}' with name '{2}'",
                            aggregateType.Name,
                            p.ParameterType.Name,
                            p.Name));
                    })
                .ToList();

            _log.Verbose(() =>
                {
                    return string.Format(
                        "Created aggregate factory for aggregate '{0}' with these arguments: {1}",
                        aggregateType.Name,
                        string.Join(", ", constructor.GetParameters().Select(p => string.Format("({0}: {1})", p.Name, p.ParameterType.Name))));
                });

            return id =>
                {
                    var arguments = argumentFactories.Select(f => f(id)).ToArray();
                    return Activator.CreateInstance(aggregateType, arguments);
                };
        }
    }
}
