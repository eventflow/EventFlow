// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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

using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Commands
{
    [Category(Categories.Unit)]
    public class CommandTests : Test
    {
        public class CriticalCommand : Command<ThingyAggregate, ThingyId, IExecutionResult>
        {
            public string CriticalData { get; }

            public CriticalCommand(ThingyId aggregateId, EventId sourceId, string criticalData) : base(aggregateId, sourceId)
            {
                CriticalData = criticalData;
            }
        }

        [Test]
        public void SerializeDeserialize()
        {
            // Arrange
            var criticalCommand = A<CriticalCommand>();

            // Act
            var json = JsonConvert.SerializeObject(criticalCommand);
            var deserialized = JsonConvert.DeserializeObject<CriticalCommand>(json);

            // Assert
            deserialized.CriticalData.Should().Be(criticalCommand.CriticalData);
            deserialized.SourceId.Should().Be(criticalCommand.SourceId);
            deserialized.AggregateId.Should().Be(criticalCommand.AggregateId);
        }
    }
}
