using System.Linq;

namespace EventFlow.Attributes;

public class ExampleDoubleDisplayValuesAttribute : ExampleDisplayValueAttribute
{
    public ExampleDoubleDisplayValuesAttribute(params double[] examples) : base(examples.ToList())
    {
    }
}
