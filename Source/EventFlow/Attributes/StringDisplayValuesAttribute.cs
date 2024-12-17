using System.Linq;

namespace EventFlow.Attributes;

public class StringDisplayValuesAttribute : DisplayValueAttribute
{
    public StringDisplayValuesAttribute(params string[] examples) : base(examples.ToList())
    {
    }
}
