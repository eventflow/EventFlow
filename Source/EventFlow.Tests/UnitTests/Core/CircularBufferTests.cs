// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using System.Linq;
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Core
{
    [Category(Categories.Unit)]
    public class CircularBufferTests
    {
        [TestCase(1)] // Below capacity
        [TestCase(1, 2)] // At capacity
        [TestCase(1, 2, 3)] // Once above capacity
        [TestCase(1, 2, 3, 4)] // Loop twice over capacity
        [TestCase(1, 2, 3, 4, 5)] // One more than of capacity
        public void Put(params int[] numbers)
        {
            // Arrange
            const int capacity = 2;
            var sut = new CircularBuffer<int>(capacity);

            // Act
            foreach (var number in numbers)
            {
                sut.Put(number);
            }

            // Assert
            var shouldContain = numbers.Reverse().Take(capacity).ToList();
            sut.Should().Contain(shouldContain);
        }

        [Test]
        public void OrderAboveCapacity()
        {
            // Arrange
            var sut = new CircularBuffer<int>(3);
            sut.Put(1);
            sut.Put(2);
            sut.Put(3);
            sut.Put(4);

            // Act
            var numbers = sut.ToArray();

            // Assert
            numbers.Should().ContainInOrder(2, 3, 4);
        }

        [Test]
        public void OrderAtCapacity()
        {
            // Arrange
            var sut = new CircularBuffer<int>(3);
            sut.Put(1);
            sut.Put(2);
            sut.Put(3);

            // Act
            var numbers = sut.ToArray();

            // Assert
            numbers.Should().ContainInOrder(1, 2, 3);
        }

        [Test]
        public void OrderBelowCapacity()
        {
            // Arrange
            var sut = new CircularBuffer<int>(3);
            sut.Put(1);
            sut.Put(2);

            // Act
            var numbers = sut.ToArray();

            // Assert
            numbers.Should().ContainInOrder(1, 2);
        }
    }
}