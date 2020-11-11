using EventFlow.Core;

namespace EventFlow.Library.Template
{
    public class EntityId : Identity<EntityId>
    {
        public EntityId(string value) : base(value) { }
    }
}
