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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Jobs;
using EventFlow.Subscribers;

namespace EventFlow.Provided.Jobs
{
    [JobVersion("DispatchToAsynchronousEventSubscribers", 1)]
    public class DispatchToAsynchronousEventSubscribersJob : IJob
    {
        public string Event { get; }
        public string Metadata { get; }

        public DispatchToAsynchronousEventSubscribersJob(
            string @event,
            string metadata)
        {
            if (string.IsNullOrEmpty(@event)) throw new ArgumentNullException(nameof(@event));
            if (string.IsNullOrEmpty(metadata)) throw new ArgumentNullException(nameof(metadata));

            Event = @event;
            Metadata = metadata;
        }

        public Task ExecuteAsync(
            IResolver resolver,
            CancellationToken cancellationToken)
        {
            var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();
            var dispatchToEventSubscribers = resolver.Resolve<IDispatchToEventSubscribers>();
            var domainEvent = eventJsonSerializer.Deserialize(Event, Metadata);

            return dispatchToEventSubscribers.DispatchToAsynchronousSubscribersAsync(
                domainEvent,
                cancellationToken);
        }

        public static DispatchToAsynchronousEventSubscribersJob Create(
            IDomainEvent domainEvent,
            IResolver resolver)
        {
            var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();
            var serializedEvent = eventJsonSerializer.Serialize(domainEvent);

            return new DispatchToAsynchronousEventSubscribersJob(
                serializedEvent.SerializedData,
                serializedEvent.SerializedMetadata);
        }
    }
}