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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates
{
    [AggregateName("Thingy")]
    public class ThingyAggregate : SnapshotAggregateRoot<ThingyAggregate, ThingyId, ThingySnapshot>,
        IEmit<ThingyDomainErrorAfterFirstEvent>
    {
        public const int SnapshotEveryVersion = 10;

        private readonly List<PingId> _pingsReceived = new List<PingId>();
        private readonly List<ThingyMessage> _messages = new List<ThingyMessage>(); 

        public bool DomainErrorAfterFirstReceived { get; private set; }
        public IReadOnlyCollection<PingId> PingsReceived => _pingsReceived;
        public IReadOnlyCollection<ThingyMessage> Messages => _messages;
        public IReadOnlyCollection<ThingySnapshotVersion> SnapshotVersions { get; private set; } = new ThingySnapshotVersion[] {};

        public ThingyAggregate(ThingyId id)
            : base(id, SnapshotEveryFewVersionsStrategy.With(SnapshotEveryVersion))
        {
            Register<ThingyPingEvent>(e => _pingsReceived.Add(e.PingId));
            Register<ThingyMessageAddedEvent>(e => _messages.Add(e.ThingyMessage));
            Register<ThingySagaStartRequestedEvent>(e => {/* do nothing */});
            Register<ThingySagaCompleteRequestedEvent>(e => {/* do nothing */});
        }

        public void DomainErrorAfterFirst()
        {
            if (DomainErrorAfterFirstReceived)
            {
                throw DomainError.With("DomainErrorAfterFirst already received!");
            }

            Emit(new ThingyDomainErrorAfterFirstEvent());
        }

        public void AddMessage(ThingyMessage message)
        {
            if (_messages.Any(m => m.Id == message.Id))
            {
                throw DomainError.With($"Thingy '{Id}' already has a message with ID '{message.Id}'");
            }

            Emit(new ThingyMessageAddedEvent(message));
        }

        public void Ping(PingId pingId)
        {
            Emit(new ThingyPingEvent(pingId));
        }

        public void RequestSagaStart()
        {
            Emit(new ThingySagaStartRequestedEvent());
        }

        public void RequestSagaComplete()
        {
            Emit(new ThingySagaCompleteRequestedEvent());
        }

        public void Apply(ThingyDomainErrorAfterFirstEvent e)
        {
            DomainErrorAfterFirstReceived = true;
        }

        protected override Task<ThingySnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ThingySnapshot(
                PingsReceived,
                Enumerable.Empty<ThingySnapshotVersion>()));
        }

        protected override Task LoadSnapshotAsync(ThingySnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
        {
            _pingsReceived.AddRange(snapshot.PingsReceived);
            SnapshotVersions = snapshot.PreviousVersions;
            return Task.FromResult(0);
        }
    }
}