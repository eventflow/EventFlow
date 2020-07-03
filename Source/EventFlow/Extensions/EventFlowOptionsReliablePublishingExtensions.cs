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

using EventFlow.Configuration;
using EventFlow.PublishRecovery;
using EventFlow.ReadStores;
using EventFlow.Subscribers;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsReliablePublishingExtensions
    {
        public static IEventFlowOptions UseReliablePublishing<TReliablePublishPersistence>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TReliablePublishPersistence : class, IReliablePublishPersistence
        {
            return eventFlowOptions
                .RegisterServices(f => f.Register<IRecoveryHandlerProcessor, RecoveryHandlerProcessor>())
                .RegisterServices(f => f.Register<IReliableMarkProcessor, ReliableMarkProcessor>())
                .RegisterServices(f => f.Register<IPublishVerificator, PublishVerificator>())
                .RegisterServices(r => r.Register<IRecoveryDetector, TimeBasedRecoveryDetector>())
                .RegisterServices(f => f.Register<IReliablePublishPersistence, TReliablePublishPersistence>(lifetime))
                .RegisterServices(f => f.Decorate<IDomainEventPublisher>(
                                      (context, inner) => new ReliableDomainEventPublisher(inner, context.Resolver.Resolve<IReliableMarkProcessor>())));
        }

        public static IEventFlowOptions UseReadModelRecoveryHandler<TReadModel, TRecoveryHandler>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TRecoveryHandler : class, IReadModelRecoveryHandler<TReadModel>
            where TReadModel : class, IReadModel
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IReadModelRecoveryHandler<TReadModel>, TRecoveryHandler>(lifetime);

                    f.Register(ctx => (IReadModelRecoveryHandler)ctx.Resolver.Resolve<IReadModelRecoveryHandler<TReadModel>>());

                    f.Decorate<IReadStoreManager>((ctx, inner) =>
                    {
                        if (inner.ReadModelType == typeof(TReadModel))
                        {
                            return new ReadStoreManagerWithErrorRecovery<TReadModel>(
                                (IReadStoreManager<TReadModel>)inner,
                                ctx.Resolver.Resolve<IReadModelRecoveryHandler<TReadModel>>());
                        }

                        return inner;
                    });
                });
        }
    }
}