// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration.Bootstraps;
using Microsoft.Extensions.Hosting;

namespace EventFlow.AspNetCore.ServiceProvider
{
    /// <summary>
    ///     Ensures that the <see cref="Bootstrapper" /> is run in an ASP.NET Core
    ///     environment when EventFlow is configured into an existing ServiceCollection
    ///     instance and <see cref="CreateResolver" /> is not used.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Local
    class HostedBootstrapper : IHostedService
    {
        private readonly IBootstrapper _bootstrapper;

        public HostedBootstrapper(IBootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _bootstrapper.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
