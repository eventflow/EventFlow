﻿// The MIT License (MIT)
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

using System;
using System.Collections.Generic;
using EventFlow.ValueObjects;

namespace EventFlow.EventStores
{
    public class GlobalSequenceNumberRange : ValueObject
    {
        public static GlobalSequenceNumberRange Range(long from, long to)
        {
            return new GlobalSequenceNumberRange(from, to);
        }

        public static IEnumerable<GlobalSequenceNumberRange> Batches(long from, long to, long batchSize)
        {
            if (from <= 0) throw new ArgumentOutOfRangeException("from");
            if (to <= 0) throw new ArgumentOutOfRangeException("to");
            if (from > to) throw new ArgumentException(string.Format(
                "The 'from' value ({0}) must be less or equal to the 'to' value ({1})",
                from,
                to));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException("batchSize");

            for (var start = from; start <= to; start = start + batchSize)
            {
                yield return Range(start, Math.Min(to, start + batchSize - 1));
            }
        }

        public long From { get; private set; }
        public long To { get; private set; }
        public long Count { get { return To - From + 1; } }

        private GlobalSequenceNumberRange(
            long from,
            long to)
        {
            if (from <= 0) throw new ArgumentOutOfRangeException("from");
            if (to <= 0) throw new ArgumentOutOfRangeException("to");
            if (from > to) throw new ArgumentException(string.Format(
                "The 'from' value ({0}) must be less or equal to the 'to' value ({1})",
                from,
                to));

            From = from;
            To = to;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return From;
            yield return To;
        }

        public override string ToString()
        {
            return string.Format("[{0},{1}]", From, To);
        }
    }
}
