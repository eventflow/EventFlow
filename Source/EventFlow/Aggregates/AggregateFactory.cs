// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Aggregates
{
    public class AggregateFactory : IAggregateFactory
    {
        private readonly IServiceProvider _serviceProvider;

        private static readonly ConcurrentDictionary<Type, AggregateConstruction> AggregateConstructions =
            new ConcurrentDictionary<Type, AggregateConstruction>();

        private readonly IMemoryCache _memoryCache;

        public AggregateFactory(
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
        }

        public Task<TAggregate> CreateNewAggregateAsync<TAggregate, TIdentity>(TIdentity id)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateConstruction = AggregateConstructions.GetOrAdd(
                typeof(TAggregate),
                _ => CreateAggregateConstruction<TAggregate, TIdentity>());

            return Task.FromResult(
                aggregateConstruction.CreateInstance<TAggregate, TIdentity>(id, _memoryCache, _serviceProvider));
        }

        private static AggregateConstruction CreateAggregateConstruction<TAggregate, TIdentity>()
        {
            var constructorInfos = typeof(TAggregate)
                .GetTypeInfo()
                .GetConstructors()
                .ToList();

            if (constructorInfos.Count != 1)
            {
                throw new ArgumentException(
                    $"Aggregate type '{typeof(TAggregate).PrettyPrint()}' doesn't have just one constructor");
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

            public TAggregate CreateInstance<TAggregate, TIdentity>(TIdentity identity,
                IMemoryCache memoryCache,
                IServiceProvider serviceProvider) where TIdentity : IIdentity
            {
                var typeOfTAggregate = typeof(TAggregate);
                switch (_parameterInfos.Count)
                {
                    case 1: // the TAggregate's constructor looks like `public TAggregate(TIdentity id)...`
                    {
                        var arg1Type = _parameterInfos.ElementAt(0).ParameterType;
                        var arg1 = arg1Type == _identityType ? identity : serviceProvider.GetRequiredService(arg1Type);
                        if (arg1Type == _identityType)
                        {
                            var createAggregateFunc = memoryCache.GetOrCreate(typeOfTAggregate,
                                _ => ReflectionHelper.CompileConstructor<TIdentity, TAggregate>());
                            return createAggregateFunc(identity);
                        }
                        else
                        {
                            var createAggregateFunc = memoryCache.GetOrCreate(typeOfTAggregate,
                                _ => ReflectionHelper.CompileConstructor<TAggregate>(typeOfTAggregate, arg1Type));
                            return createAggregateFunc(arg1);
                        }
                    }
                    case 2: // the TAggregate's constructor looks like `public TAggregate(T1 t1,T2 t2)...`
                    {
                        var arg1Type = _parameterInfos.ElementAt(0).ParameterType;
                        var arg2Type = _parameterInfos.ElementAt(1).ParameterType;
                        var arg1 = arg1Type == _identityType ? identity : serviceProvider.GetRequiredService(arg1Type);
                        var arg2 = arg2Type == _identityType ? identity : serviceProvider.GetRequiredService(arg2Type);

                        var createAggregateFunc = memoryCache.GetOrCreate(typeOfTAggregate,
                            _ => ReflectionHelper.CompileConstructor<TAggregate>(typeOfTAggregate, arg1Type, arg2Type));
                        return createAggregateFunc(arg1, arg2);
                    }
                    case 3: // the TAggregate's constructor looks like `public TAggregate(T1 t1,T2 t2,T3 t3)...`
                    {
                        var arg1Type = _parameterInfos.ElementAt(0).ParameterType;
                        var arg2Type = _parameterInfos.ElementAt(1).ParameterType;
                        var arg3Type = _parameterInfos.ElementAt(2).ParameterType;
                        var arg1 = arg1Type == _identityType ? identity : serviceProvider.GetRequiredService(arg1Type);
                        var arg2 = arg2Type == _identityType ? identity : serviceProvider.GetRequiredService(arg2Type);
                        var arg3 = arg3Type == _identityType ? identity : serviceProvider.GetRequiredService(arg3Type);

                        var createAggregateFunc = memoryCache.GetOrCreate(typeOfTAggregate,
                            _ => ReflectionHelper.CompileConstructor<TAggregate>(typeOfTAggregate,
                                arg1Type,
                                arg2Type,
                                arg3Type));
                        return createAggregateFunc(arg1, arg2, arg3);
                    }
                    case 4: // public TAggregate(T1 t1,T2 t2,T3 t3)
                    {
                        var arg1Type = _parameterInfos.ElementAt(0).ParameterType;
                        var arg2Type = _parameterInfos.ElementAt(1).ParameterType;
                        var arg3Type = _parameterInfos.ElementAt(2).ParameterType;
                        var arg4Type = _parameterInfos.ElementAt(3).ParameterType;
                        var arg1 = arg1Type == _identityType ? identity : serviceProvider.GetRequiredService(arg1Type);
                        var arg2 = arg2Type == _identityType ? identity : serviceProvider.GetRequiredService(arg2Type);
                        var arg3 = arg3Type == _identityType ? identity : serviceProvider.GetRequiredService(arg3Type);
                        var arg4 = arg4Type == _identityType ? identity : serviceProvider.GetRequiredService(arg4Type);

                        var createAggregateFunc = memoryCache.GetOrCreate(typeOfTAggregate,
                            _ => ReflectionHelper.CompileConstructor<TAggregate>(typeOfTAggregate,
                                arg1Type,
                                arg2Type,
                                arg3Type,
                                arg4Type));
                        return createAggregateFunc(arg1, arg2, arg3, arg4);
                    }
                    // Constructor's argument count>4 use reflection to create instance
                    default:
                        return (TAggregate)CreateInstance(identity, serviceProvider);
                }
            }

            private object CreateInstance(IIdentity identity,
                IServiceProvider resolver)
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
                        parameters[parameterInfo.Position] = resolver.GetRequiredService(parameterInfo.ParameterType);
                    }
                }

                return _constructorInfo.Invoke(parameters);
            }
        }
    }
}