using EventFlow.Core;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests
{
    public class PersonId : Identity<PersonId>
    {
        public PersonId(string value) : base(value)
        {
        }
    }
}