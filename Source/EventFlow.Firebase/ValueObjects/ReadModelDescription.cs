using EventFlow.ValueObjects;
using System;
using System.Collections.Generic;

namespace EventFlow.Firebase.ValueObjects
{
    public class ReadModelDescription : ValueObject
    {
        public ReadModelDescription(RootNodeName rootNodeName)
        {
            if (rootNodeName == null) throw new ArgumentNullException(nameof(rootNodeName));

            RootNodeName = rootNodeName;
        }

        public RootNodeName RootNodeName { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return RootNodeName;
        }
    }
}
