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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Snapshots.Strategies
{
    public class SnapshotRandomlyStrategy : ISnapshotStrategy
    {
        private static readonly Random Random = new Random();
        public const double DefaultChance = 0.01d;

        public static ISnapshotStrategy Default { get; } = With();

        public static ISnapshotStrategy With(
            double chance = DefaultChance)
        {
            return new SnapshotRandomlyStrategy(
                chance);
        }

        private readonly double _chance;

        private SnapshotRandomlyStrategy(
            double chance)
        {
            if (chance < 0.0d || chance > 1.0d) throw new ArgumentOutOfRangeException($"Chance '{chance}' must be between 0.0 and 1.0");

            _chance = chance;
        }

        public Task<bool> ShouldCreateSnapshotAsync(
            ISnapshotAggregateRoot snapshotAggregateRoot,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Random.NextDouble() >= _chance);
        }
    }
}