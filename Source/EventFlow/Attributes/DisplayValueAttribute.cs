using System;

namespace EventFlow.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Property |
    AttributeTargets.Enum, AllowMultiple = false)]
public class DisplayValueAttribute: Attribute, IDisplayValue
{
    public object Example { get; }

    public DisplayValueAttribute(object example) : base()
    {
        Example = example;
    }
}
