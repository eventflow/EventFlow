using EventFlow.Sagas.AggregateSagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Sagas;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.Tests.UnitTests.Sagas.AggregateSagas
{
    [Category(Categories.Unit)]
    public class AggregateSagaTests: TestsFor<ThingySaga>
    {
        [Test]
        public async Task AggregateSaga_Publish()
        {
        }
    }
}