using EventFlow.Core;
using System;
using System.Text.RegularExpressions;

namespace EventFlow.Attributes;

public class StringDisplayValueAttribute : DisplayValueAttribute
{
    public StringDisplayValueAttribute(string example) : base(example)
    {
    }
}
