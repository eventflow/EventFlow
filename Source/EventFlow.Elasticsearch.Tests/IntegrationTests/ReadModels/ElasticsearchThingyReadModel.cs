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

using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using Nest;

namespace EventFlow.Elasticsearch.Tests.IntegrationTests.ReadModels
{
    [ElasticsearchType(IdProperty = "Id", Name = "thingy")]
    public class ElasticsearchThingyReadModel : IReadModel,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>,
        IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDeletedEvent>
    {
        [Keyword(
            Index = true)]
        public string Id { get; set; }

        [Boolean(
            Name = "DomainErrorAfterFirstReceived",
            Index = false)]
        public bool DomainErrorAfterFirstReceived { get; set; }

        [Number(
            NumberType.Integer,
            Name = "PingsReceived",
            Index = false)]
        public int PingsReceived { get; set; }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity.Value;
            DomainErrorAfterFirstReceived = true;
        }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
        {
            Id = domainEvent.AggregateIdentity.Value;
            PingsReceived++;
        }

        public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyDeletedEvent> domainEvent)
        {
            context.MarkForDeletion();
        }

        public Thingy ToThingy()
        {
            return new Thingy(
                ThingyId.With(Id),
                PingsReceived,
                DomainErrorAfterFirstReceived);
        }
    }
}