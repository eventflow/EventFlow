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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Exceptions;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Strategies;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates
{
    [AggregateName("Nighty")]
    public class NightyAggregate : SnapshotAggregateRoot<NightyAggregate, NightyId, NightySnapshot>,
        IEmit<NightyDomainErrorAfterFirstEvent>,
        IEmit<NightyDeletedEvent>
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly IScopedContext _scopedContext;
        // We just hold a reference

        public const int SnapshotEveryVersion = 10;
        
        private readonly List<PingId> _pingsReceived = new List<PingId>();
        private readonly List<NightyMessage> _messages = new List<NightyMessage>(); 

        public bool DomainErrorAfterFirstReceived { get; private set; }
        public IReadOnlyCollection<PingId> PingsReceived => _pingsReceived;
        public IReadOnlyCollection<NightyMessage> Messages => _messages;
        public IReadOnlyCollection<NightySnapshotVersion> SnapshotVersions { get; private set; } = new NightySnapshotVersion[] {};
        public bool IsDeleted { get; private set; }

        public NightyAggregate(NightyId id, IScopedContext scopedContext)
            : base(id, SnapshotEveryFewVersionsStrategy.With(SnapshotEveryVersion))
        {
            _scopedContext = scopedContext;
            Register<NightyPingEvent>(e => _pingsReceived.Add(e.PingId));
            Register<NightyMessageAddedEvent>(e => _messages.Add(e.NightyMessage));
            Register<NightyMessageHistoryAddedEvent>(e => _messages.AddRange(e.NightyMessages));
            Register<NightySagaStartRequestedEvent>(e => {/* do nothing */});
            Register<NightySagaCompleteRequestedEvent>(e => {/* do nothing */});
        }

        public void DomainErrorAfterFirst()
        {
            if (DomainErrorAfterFirstReceived)
            {
                throw DomainError.With("DomainErrorAfterFirst already received!");
            }

            Emit(new NightyDomainErrorAfterFirstEvent());
        }

        public void AddMessage(NightyMessage message)
        {
            if (_messages.Any(m => m.Id == message.Id))
            {
                throw DomainError.With($"Nighty '{Id}' already has a message with ID '{message.Id}'");
            }

            Emit(new NightyMessageAddedEvent(message));
        }

        public void AddMessageHistory(NightyMessage[] messages)
        {
            var existingIds = _messages.Select(m => m.Id).Intersect(_messages.Select(m => m.Id)).ToArray();
            if (existingIds.Any())
            {
                throw DomainError.With($"Nighty '{Id}' already has messages with IDs " +
                                       $"'{string.Join(",", existingIds.Select(id => id.ToString()))}'");
            }

            Emit(new NightyMessageHistoryAddedEvent(messages));
        }

        public void Ping(PingId pingId)
        {
            Emit(new NightyPingEvent(pingId));
        }

        public IExecutionResult PingMaybe(PingId pingId, bool isSuccess)
        {
            Emit(new NightyPingEvent(pingId));
            return isSuccess
                ? ExecutionResult.Success()
                : ExecutionResult.Failed();
        }

        public void RequestSagaStart()
        {
            Emit(new NightySagaStartRequestedEvent());
        }

        public void RequestSagaComplete()
        {
            Emit(new NightySagaCompleteRequestedEvent());
        }

        public void Apply(NightyDomainErrorAfterFirstEvent e)
        {
            DomainErrorAfterFirstReceived = true;
        }

        void IEmit<NightyDeletedEvent>.Apply(NightyDeletedEvent e)
        {
            IsDeleted = true;
        }

        protected override Task<NightySnapshot> CreateSnapshotAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new NightySnapshot(
                PingsReceived,
                Enumerable.Empty<NightySnapshotVersion>()));
        }

        protected override Task LoadSnapshotAsync(NightySnapshot snapshot, ISnapshotMetadata metadata, CancellationToken cancellationToken)
        {
            _pingsReceived.AddRange(snapshot.PingsReceived);
            SnapshotVersions = snapshot.PreviousVersions;
            return Task.FromResult(0);
        }

        public void Delete()
        {
            Emit(new NightyDeletedEvent());
        }
    }
}