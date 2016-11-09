using EventFlow.ValueObjects;
using System;

namespace EventFlow.Firebase.ValueObjects
{
    public class NodeName : SingleValueObject<string>
    {
        public NodeName(string value) : base(value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        }
    }
}
