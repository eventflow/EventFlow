using EventFlow.ValueObjects;
using System;

namespace EventFlow.Firebase.ValueObjects
{
    public class RootNodeName : SingleValueObject<string>
    {
        public RootNodeName(string value) : base(value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        }
    }
}
