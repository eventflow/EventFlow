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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace EventFlow.PublishRecovery
{
    public sealed class PublishVerificator : IPublishVerificator
    {
        private const int PageSize = 200;

        private readonly IEventPersistence _eventPersistence;
        private readonly IRecoveryHandlerProcessor _recoveryHandlerProcessor;
        private readonly IEventJsonSerializer _eventSerializer;
        private readonly IRecoveryDetector _recoveryDetector;
        private readonly IReliablePublishPersistence _reliablePublishPersistence;

        public PublishVerificator(IEventPersistence eventPersistence, IRecoveryHandlerProcessor recoveryHandlerProcessor, IEventJsonSerializer eventSerializer, IRecoveryDetector recoveryDetector, IReliablePublishPersistence reliablePublishPersistence)
        {
            _eventPersistence = eventPersistence;
            _recoveryHandlerProcessor = recoveryHandlerProcessor;
            _eventSerializer = eventSerializer;
            _recoveryDetector = recoveryDetector;
            _reliablePublishPersistence = reliablePublishPersistence;
        }

        public async Task<PublishVerificationResult> VerifyOnceAsync(CancellationToken cancellationToken)
        {
            var state = await _reliablePublishPersistence.GetUnverifiedItemsAsync(PageSize, cancellationToken)
                .ConfigureAwait(false);

            var logItemLookup = state.Items.ToLookup(x => x.AggregateId);

            var page = await _eventPersistence.LoadAllCommittedEvents(state.LastVerifiedPosition, PageSize, cancellationToken)
                .ConfigureAwait(false);

            var verifyResult = VerifyDomainEvents(page, logItemLookup);

            // Some of not published events can be in flight, so no need recovery them
            // but we have to check them again on next iteration
            var eventsForRecovery = GetEventsForRecovery(verifyResult.UnpublishedEvents);

            if (eventsForRecovery.Any())
            {
                // Do it inside transaction to recover in single thread
                // success recovery should put LogItem
                await _recoveryHandlerProcessor.RecoverAfterUnexpectedShutdownAsync(eventsForRecovery, cancellationToken)
                    .ConfigureAwait(false);

                return PublishVerificationResult.RecoveredNeedVerify;
            }

            // Remove logs and move position forward only when it is successfully recovered.
            if (verifyResult.UnpublishedEvents.Count == 0)
            {
                await _reliablePublishPersistence
                    .MarkVerifiedAsync(verifyResult.PublishedLogItems, page.NextGlobalPosition, cancellationToken)
                    .ConfigureAwait(false);

                return page.CommittedDomainEvents.Count < PageSize
                    ? PublishVerificationResult.CompletedNoMoreDataToVerify
                    : PublishVerificationResult.HasMoreDataNeedVerify;
            }

            return PublishVerificationResult.CompletedNoMoreDataToVerify;
        }

        private IReadOnlyList<IDomainEvent> GetEventsForRecovery(IReadOnlyList<ICommittedDomainEvent> unpublishedEvents)
        {
            return unpublishedEvents
                .Select(evnt => _eventSerializer.Deserialize(evnt))
                .Where(evnt => _recoveryDetector.IsNeedRecovery(evnt))
                .ToList();
        }

        private VerifyResult VerifyDomainEvents(AllCommittedEventsPage page, ILookup<string, IPublishVerificationItem> logItemLookup)
        {
            var unpublishedEvents = new List<ICommittedDomainEvent>();
            var publishedLogItems = new List<IPublishVerificationItem>();

            foreach (var committedDomainEvent in page.CommittedDomainEvents)
            {
                var logItem = TryGetPublishedLogItem(committedDomainEvent, logItemLookup);

                if (logItem == null)
                {
                    unpublishedEvents.Add(committedDomainEvent);
                }
                // Remove logItem only on the last event related with this log item
                else if (logItem.IsFinalEvent(committedDomainEvent))
                {
                    publishedLogItems.Add(logItem);
                }
            }

            return new VerifyResult(unpublishedEvents, publishedLogItems);
        }

        private IPublishVerificationItem TryGetPublishedLogItem(ICommittedDomainEvent committedDomainEvent, ILookup<string, IPublishVerificationItem> logItemLookup)
        {
            var logItems = logItemLookup[committedDomainEvent.AggregateId];

            return logItems.FirstOrDefault(logItem => logItem.IsPublished(committedDomainEvent));
        }

        private sealed class VerifyResult
        {
            public VerifyResult(
                IReadOnlyList<ICommittedDomainEvent> unpublishedEvents,
                IReadOnlyList<IPublishVerificationItem> publishedLogItems)
            {
                UnpublishedEvents = unpublishedEvents;
                PublishedLogItems = publishedLogItems;
            }

            public IReadOnlyList<ICommittedDomainEvent> UnpublishedEvents { get; }

            public IReadOnlyList<IPublishVerificationItem> PublishedLogItems { get; }
        }
    }
}