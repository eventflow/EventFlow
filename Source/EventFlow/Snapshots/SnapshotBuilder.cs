// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
// 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;
using Newtonsoft.Json;

namespace EventFlow.Snapshots
{
    public class SnapshotBuilder : ISnapshotBuilder
    {
        private static readonly ConcurrentDictionary<Type, CacheItem> CacheItems = new ConcurrentDictionary<Type, CacheItem>();

        private readonly ILog _log;
        private readonly ISnapshotDefinitionService _snapshotDefinitionService;

        public SnapshotBuilder(
            ILog log,
            ISnapshotDefinitionService snapshotDefinitionService)
        {
            _log = log;
            _snapshotDefinitionService = snapshotDefinitionService;
        }

        public Task<SerializedSnapshot> BuildSnapshotAsync(
            IAggregateRoot aggregateRoot,
            CancellationToken cancellationToken)
        {
            if (!(aggregateRoot is ISnapshotAggregateRoot))
            {
                throw new ArgumentException(
                    $"Aggregate '{aggregateRoot.GetType().PrettyPrint()}' does not support snapshotting",
                    nameof(aggregateRoot));
            }

            var cacheItem = GetCacheItem(aggregateRoot.GetType());

            return cacheItem.Invoker(this, aggregateRoot, cancellationToken);
        }

        public async Task<SerializedSnapshot> BuildSnapshotAsync<TAggregate, TIdentity, TSnapshot>(
            TAggregate aggregate,
            CancellationToken cancellationToken)
            where TAggregate : ISnapshotAggregateRoot<TIdentity, TSnapshot>
            where TIdentity : IIdentity
            where TSnapshot : ISnapshot
        {
            var snapshotContainer = await aggregate.CreateSnapshotAsync(cancellationToken).ConfigureAwait(false);

            var snapsnotDefinition = _snapshotDefinitionService.GetDefinition(typeof (TSnapshot));

            var updatedSnapshotMetadata = new SnapshotMetadata(snapshotContainer.Metadata.Concat(new Dictionary<string, string>
                {
                    {SnapshotMetadataKeys.SnapshotName, snapsnotDefinition.Name},
                    {SnapshotMetadataKeys.SnapshotVersion, snapsnotDefinition.Version.ToString()},
                }));

            var serializedMetadata = JsonConvert.SerializeObject(updatedSnapshotMetadata);
            var serializedData = JsonConvert.SerializeObject(snapshotContainer.Snapshot);

            return new SerializedSnapshot(
                serializedMetadata,
                serializedData,
                aggregate.Version,
                updatedSnapshotMetadata);
        }

        private static CacheItem GetCacheItem(Type aggregateType)
        {
            return CacheItems.GetOrAdd(
                aggregateType,
                t =>
                {
                    var snapshotAggregateRootInterface = t
                        .GetInterfaces()
                        .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISnapshotAggregateRoot<,>));
                    var genericArguments = snapshotAggregateRootInterface.GetGenericArguments();
                    var indentityType = genericArguments[0];
                    var snapshotType = genericArguments[1];

                    // TODO: Use ReflectionHelper to build faster invokation

                    var genericMethodInfo = typeof (SnapshotBuilder)
                        .GetMethods()
                        .Single(mi => mi.Name == "BuildSnapshotAsync" && mi.IsGenericMethod);
                    var methodInfo = genericMethodInfo.MakeGenericMethod(aggregateType, indentityType, snapshotType);

                    return new CacheItem
                        {
                            Invoker = (builder, root, token) => (Task<SerializedSnapshot>) methodInfo.Invoke(builder, new object[] { root, token }),
                        };
                });
        }

        private class CacheItem
        {
            public Func<ISnapshotBuilder, IAggregateRoot, CancellationToken, Task<SerializedSnapshot>> Invoker { get; set; }
        }
    }
}