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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.EntityFramework.Tests.MsSql.IncludeTests;
using EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Commands;
using EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Queries;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.MsSql
{
    [Category(Categories.Integration)]
    public class EfMsSqlReadStoreIncludeTests : IntegrationTest
    {
        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            return eventFlowOptions
                .RegisterServices(sr => sr.Register(c => _testDatabase.ConnectionString))
                .ConfigureEntityFramework(EntityFrameworkConfiguration.New)
                .AddDbContextProvider<TestDbContext, MsSqlDbContextProvider>()
                .ConfigureForReadStoreIncludeTest()
                .AddDefaults(typeof(EfMsSqlReadStoreIncludeTests).Assembly)
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }

        [Test]
        public async Task ReadModelContainsPersonNameAfterCreation()
        {
            // Arrange
            var id = PersonId.New;
            
            // Act
            await CommandBus
                .PublishAsync(new CreatePersonCommand(id, "Bob"), CancellationToken.None)
                .ConfigureAwait(false);
            
            var readModel = await QueryProcessor
                .ProcessAsync(new PersonGetQuery(id), CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            readModel.Should().NotBeNull();
            readModel.Name.Should().Be("Bob");
            readModel.Addresses.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task ReadModelContainsPersonAddressesAfterAdd()
        {
            // Arrange
            var id = PersonId.New;
            await CommandBus
                .PublishAsync(new CreatePersonCommand(id, "Bob"), CancellationToken.None)
                .ConfigureAwait(false);

            // Act
            var address1 = new Address(AddressId.New, "Smith street 4.", "1234", "New York", "US");
            await CommandBus
                .PublishAsync(new AddAddressCommand(id, 
                        address1), 
                    CancellationToken.None)
                .ConfigureAwait(false);

            var address2 = new Address(AddressId.New, "Musterstraße 42.", "6541", "Berlin", "DE");
            await CommandBus
                .PublishAsync(new AddAddressCommand(id, 
                        address2), 
                    CancellationToken.None)
                .ConfigureAwait(false);

            var readModel = await QueryProcessor
                .ProcessAsync(new PersonGetQuery(id), CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            readModel.Should().NotBeNull();
            readModel.NumberOfAddresses.Should().Be(2);
            readModel.Addresses.Should().HaveCount(2);
            readModel.Addresses.Should().ContainEquivalentOf(address1);
            readModel.Addresses.Should().ContainEquivalentOf(address2);
        }
    }
}