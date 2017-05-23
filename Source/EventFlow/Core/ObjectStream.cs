// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Core
{
    public abstract class ObjectStream<T> : IObjectStream<T>
    {
        private IEnumerator<Task<IReadOnlyCollection<T>>> _enumerator;

        public async Task<IReadOnlyCollection<T>> ReadAsync(CancellationToken cancellationToken)
        {
            if (_enumerator == null)
            {
                _enumerator = Iterate().GetEnumerator();
            }

            var readOnlyCollection = await _enumerator.Current.ConfigureAwait(false);
            _enumerator.MoveNext();
            return readOnlyCollection;
        }

        protected abstract IEnumerable<Task<IReadOnlyCollection<T>>> Iterate();

        public abstract void Dispose();
    }
}