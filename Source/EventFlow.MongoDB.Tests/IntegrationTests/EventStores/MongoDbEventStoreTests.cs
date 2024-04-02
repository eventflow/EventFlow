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

using System;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using EventFlow.MongoDB.EventStore;
using EventFlow.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Mongo2Go;

namespace EventFlow.MongoDB.Tests.IntegrationTests.EventStores
{
	[Category(Categories.Integration)]
	[TestFixture]
    public class MongoDbEventStoreTests : TestSuiteForEventStore
	{
		private MongoDbRunner _runner;
		
		protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
		{
		    _runner = MongoDbRunner.Start();
		    
		    eventFlowOptions
			    .ConfigureMongoDb(_runner.ConnectionString, "eventflow")
			    .UseMongoDbEventStore();
            
		    var serviceProvider = base.Configure(eventFlowOptions);
		    
		    var eventPersistenceInitializer = serviceProvider.GetService<IMongoDbEventPersistenceInitializer>();
            eventPersistenceInitializer.Initialize();

            return serviceProvider;
		}

        [TearDown]
		public void TearDown()
		{
			_runner.Dispose();
		}
	}
}
