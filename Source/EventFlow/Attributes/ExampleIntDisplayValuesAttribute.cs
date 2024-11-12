using System.Linq;

namespace EventFlow.Attributes;

public class ExampleIntDisplayValuesAttribute : ExampleDisplayValueAttribute
{
    public ExampleIntDisplayValuesAttribute(params int[] examples) : base(examples.ToList())
    {
    }
}
