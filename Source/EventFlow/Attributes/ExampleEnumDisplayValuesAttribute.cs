using System.Linq;

namespace EventFlow.Attributes;

public class ExampleEnumDisplayValuesAttribute : ExampleDisplayValueAttribute
{
    public ExampleEnumDisplayValuesAttribute(params string[] examples) : base(examples.ToList())
    {
    }
}
