using System.Linq;

namespace EventFlow.Attributes;

public class IntDisplayValuesAttribute : DisplayValueAttribute
{
    public IntDisplayValuesAttribute(params int[] examples) : base(examples.ToList())
    {
    }
}
