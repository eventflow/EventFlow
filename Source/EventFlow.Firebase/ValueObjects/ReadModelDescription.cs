using EventFlow.ValueObjects;
using System;
using System.Collections.Generic;

namespace EventFlow.Firebase.ValueObjects
{
    public class ReadModelDescription : ValueObject
    {
        public ReadModelDescription(NodeName nodeName)
        {
            if (nodeName == null) throw new ArgumentNullException(nameof(nodeName));

            NodeName = nodeName;
        }

        public NodeName NodeName { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return NodeName;
        }
    }
}
