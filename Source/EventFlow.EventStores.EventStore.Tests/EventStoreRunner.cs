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
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores.EventStore.Tests.Extensions;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;

namespace EventFlow.EventStores.EventStore.Tests
{
    [Category(Categories.Integration)]
    public class EventStoreRunner
    {
        private static readonly Dictionary<Version, Uri> EventStoreVersions = new Dictionary<Version, Uri>
        {
            {new Version(3, 4, 0), new Uri("http://download.geteventstore.com/binaries/EventStore-OSS-Win-v3.4.0.zip", UriKind.Absolute)},
            {new Version(3, 3, 1), new Uri("http://download.geteventstore.com/binaries/EventStore-OSS-Win-v3.3.1.zip", UriKind.Absolute)}
        };

        public class EventStoreInstance : IDisposable
        {
            private readonly IDisposable _processDisposable;
            public Uri ConnectionStringUri { get; }

            public EventStoreInstance(
                Uri connectionStringUri,
                IDisposable processDisposable)
            {
                _processDisposable = processDisposable;
                ConnectionStringUri = connectionStringUri;
            }

            public void Dispose()
            {
                _processDisposable.Dispose();
            }
        }

        [Test, Explicit("Used to test the EventStore runner")]
        [Timeout(60000)]
        public async Task TestRunner()
        {
            using (await StartAsync().ConfigureAwait(false))
            {
                // Put EventStore usage here...
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
        }

        public static async Task<EventStoreInstance> StartAsync()
        {
            var eventStoreVersion = EventStoreVersions.OrderByDescending(kv => kv.Key).First();
            await InstallEventStoreAsync(eventStoreVersion.Key).ConfigureAwait(false);

            var tcpPort = TcpHelper.GetFreePort();
            var httpPort = TcpHelper.GetFreePort();
            var connectionStringUri = new Uri($"tcp://admin:changeit@{IPAddress.Loopback}:{tcpPort}");

            IDisposable processDisposable = null;
            try
            {
                processDisposable = StartExe(
                    Path.Combine(GetEventStorePath(eventStoreVersion.Key), "EventStore.ClusterNode.exe"),
                    "'admin' user added to $users",
                    "--mem-db=True",
                    "--cluster-size=1",
                    $"--ext-tcp-port={tcpPort}",
                    $"--ext-http-port={httpPort}");

                var connectionSettings = ConnectionSettings.Create()
                    .EnableVerboseLogging()
                    .KeepReconnecting()
                    .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
                    .Build();
                using (var eventStoreConnection = EventStoreConnection.Create(connectionSettings, connectionStringUri))
                {
                    var start = DateTimeOffset.Now;
                    while (true)
                    {
                        if (start + TimeSpan.FromSeconds(10) < DateTimeOffset.Now)
                        {
                            throw new Exception("Failed to connect to EventStore");
                        }

                        try
                        {
                            await eventStoreConnection.ConnectAsync().ConfigureAwait(false);
                            break;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Failed to connect, retrying");
                        }
                    }
                }
            }
            catch (Exception)
            {
                processDisposable.DisposeSafe("Failed to dispose EventStore process");
                throw;
            }

            return new EventStoreInstance(
                connectionStringUri,
                processDisposable);
        }

        private static IDisposable StartExe(
            string exePath,
            string initializationDone,
            params string[] arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(exePath, string.Join(" ", arguments))
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                }
            };
            var exeName = Path.GetFileName(exePath);
            process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                Console.WriteLine("OUT - {0}: {1}", exeName, eventArgs.Data);
            };
            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrEmpty(eventArgs.Data))
                {
                    return;
                }
                Console.WriteLine("ERR - {0}: {1}", exeName, eventArgs.Data);
            };
            Action<Process> initializeProcess = p =>
                {
                    Console.WriteLine($"{exeName} START =======================================");
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                };
            process.WaitForOutput(initializationDone, initializeProcess);

            return new DisposableAction(() =>
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(10000);
                        Console.WriteLine($"{process.ProcessName} KILLED  =======================================");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to kill process: {e.Message}");
                    }
                });
        }

        private static async Task InstallEventStoreAsync(Version version)
        {
            if (IsEventStoreInstalled(version))
            {
                Console.WriteLine($"EventStore v'{version}' is already installed");
                return;
            }

            Console.WriteLine($"EventStore v{version} not installed, installing it");

            var tempDownload = Path.Combine(
                Path.GetTempPath(),
                $"eventstore-v{version}-{Guid.NewGuid().ToString("N")}.zip");
            try
            {
                await DownloadFileAsync(EventStoreVersions[version], tempDownload).ConfigureAwait(false);
                var eventStorePath = GetEventStorePath(version);
                ExtractZipFile(tempDownload, eventStorePath);
            }
            finally
            {
                if (File.Exists(tempDownload))
                {
                    File.Delete(tempDownload);
                }
            }
        }

        private static void ExtractZipFile(string zipSourcePath, string directoryDestinationPath)
        {
            Console.WriteLine($"Extracting '{zipSourcePath}' to '{directoryDestinationPath}'");

            if (!Directory.Exists(directoryDestinationPath))
            {
                Directory.CreateDirectory(directoryDestinationPath);
            }

            ZipFile.ExtractToDirectory(zipSourcePath, directoryDestinationPath);
        }

        private static bool IsEventStoreInstalled(Version version)
        {
            return Directory.Exists(GetEventStorePath(version));
        }

        private static string GetEventStorePath(Version version)
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"eventflow-eventstore-v{version}");
        }

        private static async Task DownloadFileAsync(Uri sourceUri, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                throw new ArgumentException($"File '{destinationPath}' already exists");
            }

            Console.WriteLine($"Downloading '{sourceUri}' to '{destinationPath}'");

            using (var httpClient = new HttpClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, sourceUri))
            using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false))
            {
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to download '{sourceUri}' due to '{httpResponseMessage.StatusCode}'");
                }

                using (var sourceStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var destinationStream = new FileStream(destinationPath, FileMode.CreateNew))
                {
                    await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                }
            }
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}