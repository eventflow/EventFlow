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

using EventFlow.EntityFramework.Extensions;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.Extensions;
using EventFlow.TestHelpers.Aggregates.Entities;

namespace EventFlow.EntityFramework.Tests
{
    public static class EntityFrameworkTestExtensions
    {
        public static IEventFlowOptions ConfigureForEventStoreTest(this IEventFlowOptions options)
        {
            return options
                .UseEntityFrameworkEventStore<TestDbContext>();
        }

        public static IEventFlowOptions ConfigureForSnapshotStoreTest(this IEventFlowOptions options)
        {
            return options
                .UseEntityFrameworkSnapshotStore<TestDbContext>();
        }

        public static IEventFlowOptions ConfigureForReadStoreTest(this IEventFlowOptions options)
        {
            return options
                .RegisterServices(sr => sr.RegisterType(typeof(ThingyMessageLocator)))
                .UseEntityFrameworkReadModel<ThingyReadModelEntity, TestDbContext>()
                .UseEntityFrameworkReadModel<ThingyMessageReadModelEntity, TestDbContext, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(EfThingyGetQueryHandler),
                    typeof(EfThingyGetVersionQueryHandler),
                    typeof(EfThingyGetMessagesQueryHandler));
        }
    }
}