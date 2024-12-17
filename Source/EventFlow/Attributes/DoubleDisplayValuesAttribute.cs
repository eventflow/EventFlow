using System.Linq;

namespace EventFlow.Attributes;

public class DoubleDisplayValuesAttribute : DisplayValueAttribute
{
    public DoubleDisplayValuesAttribute(params double[] examples) : base(examples.ToList())
    {
    }
}
