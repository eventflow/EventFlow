using System.Linq;

namespace EventFlow.Attributes;

public class DisplayValuesAttribute: DisplayValueAttribute
{
    public DisplayValuesAttribute(params string[] examples) : base(examples.ToList())
    {
    }
}
