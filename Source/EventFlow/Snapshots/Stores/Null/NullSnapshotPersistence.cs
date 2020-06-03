﻿// The MIT License (MIT)
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

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Snapshots.Stores.Null
{
    public class NullSnapshotPersistence : NullSnapshotPersistence<string>, ISnapshotPersistence
    {
        public NullSnapshotPersistence(ILog log)
            : base(log)
        {
        }
    }

    public class NullSnapshotPersistence<TSerialized> : ISnapshotPersistence<TSerialized>
        where TSerialized : IEnumerable
    {
        private readonly ILog _log;

        public NullSnapshotPersistence(
            ILog log)
        {
            _log = log;
        }

        public Task<CommittedSnapshot<TSerialized>> GetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(null as CommittedSnapshot<TSerialized>);
        }

        public Task SetSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            SerializedSnapshot<TSerialized> serializedSnapshot,
            CancellationToken cancellationToken)
        {
            _log.Warning($"Trying to store aggregate snapshot '{aggregateType.PrettyPrint()}' with ID '{identity}' in the NULL store. Configure another store!");

            return Task.FromResult(0);
        }

        public Task DeleteSnapshotAsync(
            Type aggregateType,
            IIdentity identity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task PurgeSnapshotsAsync(
            Type aggregateType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task PurgeSnapshotsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }
    }
}