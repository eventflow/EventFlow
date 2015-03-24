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
using EventFlow.EventStores.MsSql;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public abstract class IntegrationTest
    {
        protected IRootResolver Resolver { get; private set; }
        protected ITestDatabase TestDatabase { get; private set; }

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
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");
            var eventFlowOptions = EventFlowOptions.New
                .ConfigureMsSql(new TestMsSqlConfiguration(TestDatabase))
                .UseEventStore<MsSqlEventStore>();
            Resolver = ConfigureEventFlow(eventFlowOptions).CreateResolver();

            EventFlowEventStoresMsSql.MigrateDatabase(Resolver.Resolve<IMsSqlDatabaseMigrator>());
        }

        [TearDown]
        public void TearDownIntegrationTest()
        {
            Resolver.Dispose();
            TestDatabase.Dispose();
        }

        protected virtual EventFlowOptions ConfigureEventFlow(EventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions;
        }
    }
}
