// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Exceptions;
using EventFlow.Test.Aggregates.Test;
using NUnit.Framework;

namespace EventFlow.Test.Suites
{
    public class EventStoreSuite<TConfiguration> : IntegrationTest<TConfiguration>
        where TConfiguration : IntegrationTestConfiguration, new()
    {
        [Test]
        public async Task OptimisticConcurrency()
        {
            var aggregate1 = EventStore.LoadAggregate<TestAggregate>("1", CancellationToken.None);
            var aggregate2 = EventStore.LoadAggregate<TestAggregate>("1", CancellationToken.None);

            aggregate1.DomainErrorAfterFirst();
            aggregate2.DomainErrorAfterFirst();

            await aggregate1.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false);
            Assert.Throws<OptimisticConcurrencyException>(async () => await aggregate2.CommitAsync(EventStore, CancellationToken.None).ConfigureAwait(false));
        }
    }
}
