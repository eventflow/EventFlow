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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Installer;
using NUnit.Framework;

namespace EventFlow.Elasticsearch.Tests
{
    [Category(Categories.Integration)]
    public class ElasticsearchRunner
    {
        private static readonly SoftwareDescription SoftwareDescription = SoftwareDescription.Create(
            "elasticsearch",
            new Version(5, 3, 2),
            "https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-5.3.2.zip");

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ClusterHealth
        {
            public string Status { get; }

            public ClusterHealth(
                string status)
            {
                Status = status;
            }
        }

        public class ElasticsearchInstance : IDisposable
        {
            private readonly IDisposable _processDisposable;

            public ElasticsearchInstance(
                Uri uri,
                IDisposable processDisposable)
            {
                Uri = uri;
                _processDisposable = processDisposable;
            }

            public Uri Uri { get; }

            public async Task<string> GetStatusAsync()
            {
                try
                {
                    var clusterHealth = await HttpHelper.GetAsAsync<ClusterHealth>(new Uri(Uri, "_cluster/health")).ConfigureAwait(false);
                    return clusterHealth.Status;
                }
                catch (Exception e)
                {
                    LogHelper.Log.Warning(e, "Exception while checking Elasticsearch health");
                    return string.Empty;
                }
            }

            public Task WaitForGreenStateAsync()
            {
                return WaitHelper.WaitAsync(TimeSpan.FromMinutes(1), async () =>
                {
                    var status = await GetStatusAsync().ConfigureAwait(false);
                    return string.Equals(status, "green", StringComparison.OrdinalIgnoreCase);
                });
            }

            public async Task DeleteEverythingAsync()
            {
                await HttpHelper.DeleteAsync(new Uri(Uri, "*")).ConfigureAwait(false);
                await WaitForGreenStateAsync().ConfigureAwait(false);
            }

            public void Dispose()
            {
                _processDisposable.Dispose();
            }
        }

        [Test, Explicit("Used to test the Elasticsearch runner")]
        [Timeout(60000)]
        public async Task TestRunner()
        {
            using (await StartAsync().ConfigureAwait(false))
            {
                // Put Elasticsearch usage here...
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
        }

        public static async Task<ElasticsearchInstance> StartAsync()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JAVA_HOME")))
                throw new InvalidOperationException("The 'JAVA_HOME' environment variable is required");

            var installedSoftware = await InstallHelper.InstallAsync(SoftwareDescription).ConfigureAwait(false);
            var version = SoftwareDescription.Version;
            var installPath = Path.Combine(installedSoftware.InstallPath,
                $"elasticsearch-{version.Major}.{version.Minor}.{version.Build}");

            var tcpPort = TcpHelper.GetFreePort();
            var exePath = Path.Combine(installPath, "bin", "elasticsearch.bat");
            var nodeName = $"node-{Guid.NewGuid():N}";

            var settings = new Dictionary<string, string>
            {
                {"node.name", nodeName},
                {"http.port", tcpPort.ToString()},
                {"gateway.expected_nodes", "1"},
                {"cluster.routing.allocation.disk.threshold_enabled", "false"}
            };
            var configFilePath = Path.Combine(installPath, "config", "elasticsearch.yml");
            if (!File.Exists(configFilePath))
            {
                throw new ApplicationException($"Could not find config file at '{configFilePath}'");
            }
            File.WriteAllLines(configFilePath, settings.Select(kv => $"{kv.Key}: {kv.Value}"));

            IDisposable processDisposable = null;
            try
            {
                processDisposable = ProcessHelper.StartExe(exePath,
                    $"[${nodeName}] started");

                var elasticsearchInstance = new ElasticsearchInstance(
                    new Uri($"http://127.0.0.1:{tcpPort}"),
                    processDisposable);

                await elasticsearchInstance.WaitForGreenStateAsync().ConfigureAwait(false);
                await elasticsearchInstance.DeleteEverythingAsync().ConfigureAwait(false);

                return elasticsearchInstance;
            }
            catch
            {
                processDisposable.DisposeSafe("Failed to dispose Elasticsearch process");
                throw;
            }
        }
    }
}