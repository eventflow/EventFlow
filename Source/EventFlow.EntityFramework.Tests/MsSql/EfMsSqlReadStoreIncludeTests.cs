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

            var address2 = new Address(AddressId.New, "Musterstraﬂe 42.", "6541", "Berlin", "DE");
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