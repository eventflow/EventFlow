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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.Aggregates
{
    public class AggregateFactory : IAggregateFactory
    {
        private readonly IResolver _resolver;
        private static readonly ConcurrentDictionary<Type, AggregateConstruction> AggregateConstructions = new ConcurrentDictionary<Type, AggregateConstruction>();

        public AggregateFactory(
            IResolver resolver)
        {
            _resolver = resolver;
        }

        public Task<TAggregate> CreateNewAggregateAsync<TAggregate, TIdentity>(TIdentity id)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateConstruction = AggregateConstructions.GetOrAdd(
                typeof(TAggregate),
                _ => CreateAggregateConstruction<TAggregate, TIdentity>());

            var aggregate = aggregateConstruction.CreateInstance(id, _resolver);

            return Task.FromResult((TAggregate)aggregate);
        }

        private static AggregateConstruction CreateAggregateConstruction<TAggregate, TIdentity>()
        {
            var constructorInfos = typeof(TAggregate)
                .GetTypeInfo()
                .GetConstructors()
                .ToList();

            if (constructorInfos.Count != 1)
            {
                throw new ArgumentException($"Aggregate type '{typeof(TAggregate).PrettyPrint()}' doesn't have just one constructor");
            }

            var constructorInfo = constructorInfos.Single();

            var parameterInfos = constructorInfo.GetParameters();
            var identityType = typeof(TIdentity);

            return new AggregateConstruction(
                parameterInfos,
                constructorInfo,
                identityType);
        }

        private class AggregateConstruction
        {
            private readonly IReadOnlyCollection<ParameterInfo> _parameterInfos;
            private readonly ConstructorInfo _constructorInfo;
            private readonly Type _identityType;

            public AggregateConstruction(
                IReadOnlyCollection<ParameterInfo> parameterInfos,
                ConstructorInfo constructorInfo,
                Type identityType)
            {
                _parameterInfos = parameterInfos;
                _constructorInfo = constructorInfo;
                _identityType = identityType;
            }

            public object CreateInstance(IIdentity identity, IResolver resolver)
            {
                var parameters = new object[_parameterInfos.Count];
                foreach (var parameterInfo in _parameterInfos)
                {
                    if (parameterInfo.ParameterType == _identityType)
                    {
                        parameters[parameterInfo.Position] = identity;
                    }
                    else
                    {
                        parameters[parameterInfo.Position] = resolver.Resolve(parameterInfo.ParameterType);
                    }
                }

                return _constructorInfo.Invoke(parameters);
            }
        }
    }
}