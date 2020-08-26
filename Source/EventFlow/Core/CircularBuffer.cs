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
using System.Collections;
using System.Collections.Generic;

namespace EventFlow.Core
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private int _start;
        private int _end;

        public int Capacity => _buffer.Length - 1;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException(nameof(capacity));

            _buffer = new T[capacity + 1];
            _start = 0;
            _end = 0;
        }

        public CircularBuffer(int capacity, params T[] items)
            : this(capacity)
        {
            if (items.Length > capacity) throw new ArgumentException(nameof(capacity));

            foreach (var item in items)
            {
                Put(item);
            }
        }

        public void Put(T item)
        {
            _buffer[_end] = item;
            _end = (_end + 1) % _buffer.Length;
            if (_end == _start)
            {
                _start = (_start + 1) % _buffer.Length;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var i = _start;
            while (i != _end)
            {
                yield return _buffer[i];
                i = (i + 1) % _buffer.Length;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
