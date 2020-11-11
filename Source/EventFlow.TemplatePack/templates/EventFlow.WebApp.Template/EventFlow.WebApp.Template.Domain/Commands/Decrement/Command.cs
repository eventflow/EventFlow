using EventFlow.Commands;

namespace EventFlow.WebApp.Template.Domain.Commands.Decrement
{
    public class Command : Command<Entity, EntityId>
    {
        public Command(EntityId aggregateId) : base(aggregateId) { }
    }
}
