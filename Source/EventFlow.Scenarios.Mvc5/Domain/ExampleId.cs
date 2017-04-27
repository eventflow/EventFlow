using EventFlow.Core;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleId : Identity<ExampleId>
    {
        public ExampleId(string value) : base(value) { }
    }
}