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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Installer;
using NUnit.Framework;

namespace EventFlow.Redis.Tests
{
    [Category(Categories.Integration)]
    public class RedisRunner
    {
        private static readonly SoftwareDescription SoftwareDescription = SoftwareDescription.Create(
            "redis",
            new Version(3, 2, 100),
            "https://github.com/MicrosoftArchive/redis/releases/download/win-3.2.100/Redis-x64-3.2.100.zip");

        public static async Task<RedisInstance> StartAsync()
        {
            var installedSoftware = await InstallHelper.InstallAsync(SoftwareDescription).ConfigureAwait(false);
            var exePath = Path.Combine(installedSoftware.InstallPath, "redis-server.exe");

            IDisposable processDisposable = null;
            try
            {
                processDisposable = ProcessHelper.StartExe(exePath,
                    $"Server started");

                var elasticsearchInstance = new RedisInstance(processDisposable);

                return elasticsearchInstance;
            }
            catch
            {
                processDisposable.DisposeSafe("Failed to dispose Redis process");
                throw;
            }
        }
        
        [Test, Explicit("Used to test the Redis runner")]
        [Timeout(60000)]
        public async Task TestRunner()
        {
            using (await StartAsync().ConfigureAwait(false))
            {
                // Put Redis usage here...
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
        }

        public class RedisInstance : IDisposable
        {
            private readonly IDisposable _processDisposable;

            public RedisInstance(IDisposable processDisposable)
            {
                _processDisposable = processDisposable;
            }
            
            public void Dispose()
            {
                _processDisposable.Dispose();
            }
        }
    }
}