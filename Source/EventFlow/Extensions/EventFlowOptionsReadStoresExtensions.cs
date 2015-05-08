// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using EventFlow.Aggregates;
using EventFlow.Configuration.Registrations;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsReadStoresExtensions
    {
        public static EventFlowOptions UseInMemoryReadStoreFor<TAggregate, TIdentity, TReadModel>(
            this EventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModel : IReadModel, new()
        {
            eventFlowOptions.AddReadModelStore<TAggregate, TIdentity, IInMemoryReadModelStore<TAggregate, TIdentity, TReadModel>>();
            eventFlowOptions.RegisterServices(f => f.Register<IInMemoryReadModelStore<TAggregate, TIdentity, TReadModel>, InMemoryReadModelStore<TAggregate, TIdentity, TReadModel>>(Lifetime.Singleton));
            return eventFlowOptions;
        }

        public static EventFlowOptions AddReadModelStore<TAggregate, TIdentity, TReadModelStore>(
            this EventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModelStore : class, IReadModelStore<TAggregate, TIdentity>
        {
            if (typeof(TReadModelStore).IsInterface)
            {
                eventFlowOptions.RegisterServices(f => f.Register<IReadModelStore<TAggregate, TIdentity>>(r => r.Resolver.Resolve<TReadModelStore>(), lifetime));
            }
            else
            {
                eventFlowOptions.RegisterServices(f => f.Register<IReadModelStore<TAggregate, TIdentity>, TReadModelStore>(lifetime));
            }

            return eventFlowOptions;
        }
    }
}
