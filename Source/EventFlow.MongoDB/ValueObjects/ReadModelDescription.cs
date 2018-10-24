using EventFlow.ValueObjects;
using System;
using System.Collections.Generic;

namespace EventFlow.MongoDB.ValueObjects
{
    public class ReadModelDescription : ValueObject
    {
        public ReadModelDescription(RootCollectionName rootCollectionName)
        {
            if (rootCollectionName == null) throw new ArgumentNullException(nameof(rootCollectionName));

            RootCollectionName = rootCollectionName;
        }

        public RootCollectionName RootCollectionName { get; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return RootCollectionName;
        }
    }
}
