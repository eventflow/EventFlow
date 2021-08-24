using EventFlow.Core;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests
{
    public class AddressId : Identity<AddressId>
    {
        public AddressId(string value) : base(value)
        {
        }
    }
}