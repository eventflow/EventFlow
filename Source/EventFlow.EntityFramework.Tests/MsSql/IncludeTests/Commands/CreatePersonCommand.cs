using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Commands
{
    public class CreatePersonCommand : Command<PersonAggregate,PersonId>
    {
        public string Name { get; }

        public CreatePersonCommand(PersonId aggregateId, string name)
            :base(aggregateId)
        {
            Name = name;
        }
    }

    public class CreatePersonCommandHandler : CommandHandler<PersonAggregate, PersonId, CreatePersonCommand>
    {
        public override Task ExecuteAsync(PersonAggregate aggregate, CreatePersonCommand command, CancellationToken cancellationToken)
        {
            aggregate.Create(command.Name);
            return Task.CompletedTask;
        }
    }
}