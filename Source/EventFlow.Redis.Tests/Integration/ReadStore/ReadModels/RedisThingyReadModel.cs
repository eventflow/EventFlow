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

using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.Redis.ReadStore;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.Redis.Tests.Integration.ReadStore.ReadModels;

public class RedisThingyReadModel : RedisReadModel,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>,
    IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDeletedEvent>
{
    public bool DomainErrorAfterFirstReceived { get; set; }
    public int PingsReceived { get; set; }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyDeletedEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        context.MarkForDeletion();

        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        DomainErrorAfterFirstReceived = true;

        return Task.CompletedTask;
    }

    public Task ApplyAsync(IReadModelContext context,
        IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
        CancellationToken cancellationToken)
    {
        Id = domainEvent.AggregateIdentity.Value;
        PingsReceived++;

        return Task.CompletedTask;
    }

    public Thingy ToThingy()
    {
        return new Thingy(
            ThingyId.With(Id),
            PingsReceived,
            DomainErrorAfterFirstReceived);
    }
}