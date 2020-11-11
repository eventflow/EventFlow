using EventFlow.Commands;

namespace EventFlow.WebApp.Template.Domain.Commands.Increment
{
    public class Command : Command<Entity, EntityId>
    {
        public Command(EntityId aggregateId) : base(aggregateId) { }
    }
}
