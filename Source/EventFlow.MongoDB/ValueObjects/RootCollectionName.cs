using EventFlow.ValueObjects;
using System;

namespace EventFlow.MongoDB.ValueObjects
{
    public class RootCollectionName : SingleValueObject<string>
    {
        public RootCollectionName(string value) : base(value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        }
    }
}
