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

using System.Linq;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Events;
using Nest;

namespace EventFlow.Elasticsearch.Tests.IntegrationTests.ReadModels
{
    [ElasticsearchType(IdProperty = "Id", Name = "message")]
    public class ElasticsearchThingyMessageReadModel : IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageAddedEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyMessageHistoryAddedEvent>
    {
        public string Id { get; set; }

        [Keyword(
            Name = "ThingyId",
            Index = true)]
        public string ThingyId { get; set; }

        [Text(
            Name = "Message",
            Index = false)]
        public string Message { get; set; }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageAddedEvent> domainEvent)
        {
            ThingyId = domainEvent.AggregateIdentity.Value;

            var thingyMessage = domainEvent.AggregateEvent.ThingyMessage;
            Id = thingyMessage.Id.Value;
            Message = thingyMessage.Message;
        }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyMessageHistoryAddedEvent> domainEvent)
        {
            ThingyId = domainEvent.AggregateIdentity.Value;

            var messageId = new ThingyMessageId(context.ReadModelId);
            var thingyMessage = domainEvent.AggregateEvent.ThingyMessages.Single(m => m.Id == messageId);
            Id = messageId.Value;
            Message = thingyMessage.Message;
        }

        public ThingyMessage ToThingyMessage()
        {
            return new ThingyMessage(
                ThingyMessageId.With(Id),
                Message);
        }
    }
}