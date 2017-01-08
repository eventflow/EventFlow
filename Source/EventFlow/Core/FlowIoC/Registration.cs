// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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

using EventFlow.Configuration;

namespace EventFlow.Core.FlowIoC
{
    internal class Registration
    {
        private readonly IFactory _factory;
        private readonly Lifetime _lifetime;
        private readonly object _syncRoot = new object();
        private object _singleton;

        public Registration(
            Lifetime lifetime,
            IFactory factory)
        {
            _factory = factory;
            _lifetime = lifetime;
        }

        public object Create(IResolverContext resolverContext)
        {
            if (_lifetime == Lifetime.AlwaysUnique)
            {
                return _factory.Create(resolverContext);
            }

            if (_singleton == null)
            {
                lock (_syncRoot)
                {
                    if (_singleton == null)
                    {
                        _singleton = _factory.Create(resolverContext);
                    }
                }
            }

            return _singleton;
        }
    }
}