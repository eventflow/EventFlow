﻿using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    [Category(Categories.Integration)]
    public class EfSqliteEventStoreTests : TestSuiteForEventStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureForEventStoreTest<SqliteDbContextProvider>()
                .CreateResolver();
        }
    }
}