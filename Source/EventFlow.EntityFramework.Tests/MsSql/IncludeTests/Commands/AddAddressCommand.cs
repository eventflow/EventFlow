using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Commands
{
    public class AddAddressCommand : Command<PersonAggregate, PersonId>
    {
        public Address PersonAddress { get; }

        public AddAddressCommand(PersonId aggregateId, Address personAddress) : base(aggregateId)
        {
            PersonAddress = personAddress;
        }
    }

    public class AddAddressCommandHandler : CommandHandler<PersonAggregate, PersonId, AddAddressCommand>
    {
        public override Task ExecuteAsync(PersonAggregate aggregate, AddAddressCommand command, CancellationToken cancellationToken)
        {
            aggregate.AddAddress(command.PersonAddress);
            return Task.CompletedTask;
        }
    }
}