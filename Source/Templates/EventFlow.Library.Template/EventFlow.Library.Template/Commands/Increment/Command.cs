using EventFlow.Commands;

namespace EventFlow.Library.Template.Commands.Increment
{
    public class Command : Command<Entity, EntityId>
    {
        public Command(EntityId aggregateId) : base(aggregateId) { }
    }
}
