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
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [Category(Categories.Unit)]
    public class AggregateRootApplyEventTests
    {
        [Test]
        public void EventApplier()
        {
           // Arrange
           var myAggregate = new MyAggregate(MyId.New);
            
            // Act
            myAggregate.Count(42);
            
            // Assert
            myAggregate.State.Count.Should().Be(42);
        }


        private class MyAggregate : AggregateRoot<MyAggregate, MyId>
        {
            public MyState State { get; private set; }

            public MyAggregate(MyId id) : base(id)
            {
                State = new MyState();
                Register(State);
            }

            public void Count(int count)
            {
                Emit(new MyCountEvent(count));
            }
        }

        private class MyState: IEventApplier<MyAggregate, MyId>
        {
            public int Count { get; private set; }

            public bool Apply(MyAggregate aggregate, IAggregateEvent<MyAggregate, MyId> aggregateEvent)
            {
                var myCountEvent = (MyCountEvent)aggregateEvent;
                Count += myCountEvent.Count;
                return true;
            }
        }

        public class MyCountEvent : IAggregateEvent<MyAggregate, MyId>
        {
            public int Count { get; private set; }

            public MyCountEvent(int count)
            {
                Count = count;
            }
        }

        private class MyId : Identity<MyId>
        {
            public MyId(string value) : base(value) { }
        }
    }
}