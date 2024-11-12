using System;

namespace EventFlow.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Property |
    AttributeTargets.Enum, AllowMultiple = false)]
public class ExampleDisplayValueAttribute: Attribute, IExampleDisplayValue
{
    public object Example { get; }

    public ExampleDisplayValueAttribute(object example) : base()
    {
        Example = example;
    }
}
