// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
// 
using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.TestHelpers.Aggregates.Test.Entities;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates.Test
{
    [AggregateName("Test")]
    public class TestAggregate : AggregateRoot<TestAggregate, TestId>,
        IEmit<DomainErrorAfterFirstEvent>
    {
        private readonly List<PingId> _pingsReceived = new List<PingId>();
        private readonly List<TestItem> _testItems = new List<TestItem>(); 

        public bool DomainErrorAfterFirstReceived { get; private set; }
        public IReadOnlyCollection<PingId> PingsReceived => _pingsReceived;
        public IReadOnlyCollection<TestItem> TestItems => _testItems; 

        public TestAggregate(TestId id) : base(id)
        {
            Register<PingEvent>(e => _pingsReceived.Add(e.PingId));
            Register<ItemAddedEvent>(e => _testItems.Add(e.TestItem));
        }

        public void DomainErrorAfterFirst()
        {
            if (DomainErrorAfterFirstReceived)
            {
                throw DomainError.With("DomainErrorAfterFirst already received!");
            }

            Emit(new DomainErrorAfterFirstEvent());
        }

        public void Ping(PingId pingId)
        {
            Emit(new PingEvent(pingId));
        }

        public void AddItem(TestItem testItem)
        {
            Emit(new ItemAddedEvent(testItem));
        }

        public void Apply(DomainErrorAfterFirstEvent e)
        {
            DomainErrorAfterFirstReceived = true;
        }
    }
}