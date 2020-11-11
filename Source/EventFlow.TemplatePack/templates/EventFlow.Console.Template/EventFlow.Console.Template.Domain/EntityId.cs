using EventFlow.Core;
using System;

namespace EventFlow.Console.Template.Domain
{
    public class EntityId : Identity<EntityId>
    {
        public static string AsIdentity(Guid id)
        {
            return $"Entity-{id}".ToLower();
        }

        public EntityId(Guid value) : base(AsIdentity(value)) { }

        public EntityId(string value) : base(value) { }
    }
}
