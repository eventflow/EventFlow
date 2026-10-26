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
using EventFlow.EventStores.KurrentDB.Extensions;
using EventFlow.Extensions;
using KurrentDB.Client;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.EventStores.KurrentDB.Tests.UnitTests
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class EventFlowOptionsExtensionsTests
    {
        // No database is required: configuring and resolving services must not connect.
        private const string ConnectionString = "kurrentdb://localhost:1?tls=false";

        [Test]
        public void RegistrationReturnsTheSameOptionsForChaining()
        {
            var options = EventFlowOptions.New;
            Assert.That(options.UseKurrentDBEventStore(ConnectionString), Is.SameAs(options));
        }

        [Test]
        public void RegistrationSelectsKurrentDBPersistence()
        {
            using (var resolver = EventFlowOptions.New.UseKurrentDBEventStore(ConnectionString).CreateResolver())
            {
                Assert.That(resolver.Resolve<IEventPersistence>(), Is.TypeOf<KurrentDBEventPersistence>());
                Assert.That(resolver.Resolve<IEventStore>(), Is.TypeOf<EventStoreBase>());
            }
        }

        [Test]
        public void ClientIsSharedAcrossResolutionsAndScopes()
        {
            using (var resolver = EventFlowOptions.New.UseKurrentDBEventStore(ConnectionString).CreateResolver())
            {
                var client = resolver.Resolve<KurrentDBClient>();
                Assert.That(resolver.Resolve<KurrentDBClient>(), Is.SameAs(client));
                using (var first = resolver.BeginScope())
                using (var second = resolver.BeginScope())
                {
                    Assert.That(first.Resolve<KurrentDBClient>(), Is.SameAs(client));
                    Assert.That(second.Resolve<KurrentDBClient>(), Is.SameAs(client));
                    Assert.That(first.Resolve<IEventPersistence>(), Is.TypeOf<KurrentDBEventPersistence>());
                    Assert.That(second.Resolve<IEventPersistence>(), Is.TypeOf<KurrentDBEventPersistence>());
                }
                Assert.That(resolver.Resolve<KurrentDBClient>(), Is.SameAs(client));
            }
        }

        [Test]
        public void EachRootResolverOwnsADifferentClient()
        {
            var options = EventFlowOptions.New.UseKurrentDBEventStore(ConnectionString);
            using (var first = options.CreateResolver())
            using (var second = options.CreateResolver())
            {
                Assert.That(first.Resolve<KurrentDBClient>(), Is.Not.SameAs(second.Resolve<KurrentDBClient>()));
            }
        }

        [Test]
        public void NullOptionsAreRejected()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                EventFlowOptionsExtensions.UseKurrentDBEventStore(null, ConnectionString));
            Assert.That(exception.ParamName, Is.EqualTo("eventFlowOptions"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void EmptyConnectionStringsAreRejected(string connectionString)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                EventFlowOptions.New.UseKurrentDBEventStore(connectionString));
            Assert.That(exception.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void MalformedConnectionStringIsRejectedDuringConfiguration()
        {
            Assert.That(() => EventFlowOptions.New.UseKurrentDBEventStore("not a connection string"), Throws.Exception);
        }
    }
}
