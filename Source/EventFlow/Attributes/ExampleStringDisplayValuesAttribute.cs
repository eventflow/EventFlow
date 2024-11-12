using System.Linq;

namespace EventFlow.Attributes;

public class ExampleStringDisplayValuesAttribute : ExampleDisplayValueAttribute
{
    public ExampleStringDisplayValuesAttribute(params string[] examples) : base(examples.ToList())
    {
    }
}
