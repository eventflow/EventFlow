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
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace EventFlow.TestHelpers
{
    public class TcpHelper
    {
        private static readonly Random Random = new Random();

        public static int GetFreePort()
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var usedPorts = Enumerable.Empty<int>()
                .Concat(ipGlobalProperties.GetActiveTcpListeners().Select(l => l.Port))
                .Concat(ipGlobalProperties.GetActiveUdpListeners().Select(l => l.Port))
                .Concat(ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint.Port))
                .Distinct();

            var portLookup = new HashSet<int>(usedPorts);

            while (true)
            {
                var port = Random.Next(10000, 60000);
                if (!portLookup.Contains(port))
                {
                    return port;
                }
            }
        }
    }
}