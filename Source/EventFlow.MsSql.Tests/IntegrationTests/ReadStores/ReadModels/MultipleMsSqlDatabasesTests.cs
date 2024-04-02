// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Extensions;
using EventFlow.ReadStores;
using EventFlow.Sql.Migrations;
using EventFlow.Sql.ReadModels.Attributes;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Extensions;
using EventFlow.TestHelpers.MsSql;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.ReadStores.ReadModels
{
    public class MultipleMsSqlDatabasesTests : Test
    {
        private IMsSqlDatabase _eventDatabase;
        private IMsSqlDatabase _readModelDatabase;
        private ServiceProvider _serviceProvider;
        private ICommandBus _commandBus;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _eventDatabase = MsSqlHelpz.CreateDatabase("events");
            _readModelDatabase = MsSqlHelpz.CreateDatabase("read-models");

            var sqlScript = new SqlScript(
                "MagicReadModel",
                @"
                    CREATE TABLE [dbo].[ReadModel-Magic](
	                    [Id] [bigint] IDENTITY(1,1) NOT NULL,
	                    [Version] [bigint] NOT NULL,
	                    [MagicId] [nvarchar](64) NOT NULL,
	                    [Message] [nvarchar](MAX) NOT NULL,
	                    CONSTRAINT [PK_ReadModel-Magic] PRIMARY KEY CLUSTERED 
	                    (
		                    [Id] ASC
	                    )
                    )

                    CREATE UNIQUE NONCLUSTERED INDEX [IX_ReadModel-Magic_MagicId] ON [dbo].[ReadModel-Magic]
                    (
	                    [MagicId] ASC
                    )");

            _serviceProvider = EventFlowOptions.New()
                .AddEvents(new []{typeof(MagicEvent)})
                .AddCommands(new []{typeof(MagicCommand)})
                .AddCommandHandlers(typeof(MagicCommandHandler))
                .ConfigureMsSql(MsSqlConfiguration.New
                    .SetConnectionString(_eventDatabase.ConnectionString.Value)
                    .SetConnectionString("read-models", _readModelDatabase.ConnectionString.Value))
                .UseMssqlReadModel<MagicReadModel>()
                .ServiceCollection.BuildServiceProvider(true);

            var msSqlDatabaseMigrator = _serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();

            // Create aggregate store table
            EventFlowEventStoresMsSql.MigrateDatabaseAsync(msSqlDatabaseMigrator, CancellationToken.None).Wait();

            // Create read model store table
            msSqlDatabaseMigrator.MigrateDatabaseUsingScriptsAsync(
                "read-models",
                new[] { sqlScript },
                CancellationToken.None).Wait();

            _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _serviceProvider.Dispose();
            _readModelDatabase.Dispose();
            _eventDatabase.Dispose();
        }

        [Test]
        public async Task MultipleDatabases()
        {
            // Arrange
            var magicId = MagicId.New;
            var expectedMessage = A<string>();

            // Act
            await _commandBus.PublishAsync(new MagicCommand(magicId, A<string>()));
            await _commandBus.PublishAsync(new MagicCommand(magicId, expectedMessage));

            // Assert
            var fetchedMagicReadModels = _readModelDatabase.Query<MagicReadModel>(
                "SELECT * FROM [ReadModel-Magic] WHERE [MagicId] = @Id",
                new { Id = magicId.Value });
            fetchedMagicReadModels.Should().HaveCount(1);
            var fetchedMagicReadModel = fetchedMagicReadModels.Single();
            fetchedMagicReadModel.Message.Should().Be(expectedMessage);
            fetchedMagicReadModel.Version.Should().Be(2);
        }

        public class MagicId : Identity<MagicId>
        {
            public MagicId(string value) : base(value) { }
        }

        public class MagicAggregate : AggregateRoot<MagicAggregate, MagicId>,
            IEmit<MagicEvent>
        {
            public string Message { get; set; } = string.Empty;

            public MagicAggregate(MagicId id) : base(id) { }

            public void Magic(string message)
            {
                Emit(new MagicEvent(message));
            }

            public void Apply(MagicEvent magicEvent)
            {
                Message = magicEvent.Message;
            }
        }

        public class MagicEvent : AggregateEvent<MagicAggregate, MagicId>
        {
            public string Message { get; }

            public MagicEvent(
                string message)
            {
                Message = message;
            }
        }

        public class MagicCommand : Command<MagicAggregate, MagicId>
        {
            public string Message { get; }

            public MagicCommand(MagicId aggregateId, string message) : base(aggregateId)
            {
                Message = message;
            }
        }

        public class MagicCommandHandler : CommandHandler<MagicAggregate, MagicId, MagicCommand>
        {
            public override Task ExecuteAsync(
                MagicAggregate aggregate,
                MagicCommand command,
                CancellationToken _)
            {
                aggregate.Magic(command.Message);
                return Task.CompletedTask;
            }
        }

        [SqlReadModelConnectionStringName("read-models")]
        public class MagicReadModel : IReadModel, IAmReadModelFor<MagicAggregate, MagicId, MagicEvent>
        {
            [SqlReadModelIdentityColumn]
            public string MagicId { get; set; }

            [SqlReadModelVersionColumn]
            public int Version { get; set; }

            public string Message { get; set; }

            public Task ApplyAsync(
                IReadModelContext context,
                IDomainEvent<MagicAggregate, MagicId, MagicEvent> domainEvent,
                CancellationToken _)
            {
                MagicId = domainEvent.AggregateIdentity.Value;
                Message = domainEvent.AggregateEvent.Message;
                return Task.CompletedTask;
            }
        }
    }
}
