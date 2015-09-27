// The MIT License (MIT)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EventFlow.Core;

namespace EventFlow.Configuration.Registrations
{
    public class AutofacStartable : IStartable
    {
        private readonly IReadOnlyCollection<IBootstrap> _bootstraps;

        public AutofacStartable(
            IEnumerable<IBootstrap> bootstraps)
        {
            _bootstraps = OrderBootstraps(bootstraps);
        }

        public void Start()
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(StartAsync(CancellationToken.None));
            }
        }

        private Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_bootstraps.Select(b => b.BootAsync(cancellationToken)));
        }

        private static IReadOnlyCollection<IBootstrap> OrderBootstraps(IEnumerable<IBootstrap> bootstraps)
        {
            var list = bootstraps
                .Select(b => new
                    {
                        Bootstrap = b,
                        AssemblyName = b.GetType().Assembly.GetName().Name,
                    })
                .ToList();
            var eventFlowBootstraps = list
                .Where(a => a.AssemblyName.StartsWith("EventFlow"))
                .OrderBy(a => a.AssemblyName)
                .Select(a => a.Bootstrap);
            var otherBootstraps = list
                .Where(a => !a.AssemblyName.StartsWith("EventFlow"))
                .OrderBy(a => a.AssemblyName)
                .Select(a => a.Bootstrap);
            return eventFlowBootstraps.Concat(otherBootstraps).ToList();
        }
    }
}