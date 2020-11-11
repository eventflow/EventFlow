using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using System.Threading.Tasks;

namespace EventFlow.Console.Template.Domain
{
    public class Entity : AggregateRoot<Entity, EntityId>
    {
        private readonly EntityState _state = new EntityState();

        public Entity(EntityId id) : base(id) 
        {
            Register(_state);
        }

        public Task<IExecutionResult> Increment()
        {
            Emit(new Events.Incremented());

            return Task.FromResult(ExecutionResult.Success());
        }

        public Task<IExecutionResult> Decrement()
        {
            Emit(new Events.Decremented());

            return Task.FromResult(ExecutionResult.Success());
        }
    }
}
