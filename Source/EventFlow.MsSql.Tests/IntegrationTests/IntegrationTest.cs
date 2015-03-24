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

using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.EventStores.MsSql;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.Test;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public abstract class IntegrationTest
    {
        protected IFixture Fixture { get; private set; }
        protected IRootResolver Resolver { get; private set; }
        protected ITestDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }
        protected ICommandBus CommandBus { get; private set; }
        protected IEventStore EventStore { get; private set; }

        private class TestMsSqlConfiguration : IMsSqlConfiguration
        {
            private readonly ITestDatabase _testDatabase;
            public string ConnectionString { get { return _testDatabase.ConnectionString; } }

            public TestMsSqlConfiguration(ITestDatabase testDatabase)
            {
                _testDatabase = testDatabase;
            }
        }

        [SetUp]
        public void SetUpIntegrationTest()
        {
            Fixture = new Fixture();
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");
            var eventFlowOptions = EventFlowOptions.New
                .AddEvents(EventFlowTest.Assembly)
                .ConfigureMsSql(new TestMsSqlConfiguration(TestDatabase))
                .UseEventStore<MsSqlEventStore>();
            Resolver = ConfigureEventFlow(eventFlowOptions).CreateResolver();

            EventFlowEventStoresMsSql.MigrateDatabase(Resolver.Resolve<IMsSqlDatabaseMigrator>());

            MsSqlConnection = Resolver.Resolve<IMsSqlConnection>();
            CommandBus = Resolver.Resolve<ICommandBus>();
            EventStore = Resolver.Resolve<IEventStore>();
        }

        [TearDown]
        public void TearDownIntegrationTest()
        {
            Resolver.Dispose();
            TestDatabase.Dispose();
        }

        protected T A<T>() { return Fixture.Create<T>(); }

        protected virtual EventFlowOptions ConfigureEventFlow(EventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions;
        }
    }
}
