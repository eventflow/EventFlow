// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores.EventStore.Tests.Extensions;
using NUnit.Framework;

namespace EventFlow.EventStores.EventStore.Tests
{
    public class EventStoreRunner
    {
        private static readonly Dictionary<Version, Uri> EventStoreVersions = new Dictionary<Version, Uri>
        {
            {new Version(3, 4, 0), new Uri("http://download.geteventstore.com/binaries/EventStore-OSS-Win-v3.4.0.zip", UriKind.Absolute)}
        };

        [Test]
        [Timeout(60000)]
        public async Task TestRunner()
        {
            using (var eventStore = await StartAsync().ConfigureAwait(false))
            {
                // Put EventStore usage here...
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        public static async Task<IDisposable> StartAsync()
        {
            var eventStoreVersion = EventStoreVersions.OrderByDescending(kv => kv.Key).First();
            await InstallEventStoreAsync(eventStoreVersion.Key).ConfigureAwait(false);

            return StartExe(
                Path.Combine(GetEventStorePath(eventStoreVersion.Key), "EventStore.ClusterNode.exe"),
                "'admin' user added to $users",
                "--mem-db",
                "--cluster-size 1");
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
            process.ErrorDataReceived += (sender, eventArgs) => Console.WriteLine("ERR - {0}: {1}", exeName, eventArgs.Data);
            Action<Process> initializeProcess = p =>
                {
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
                    }
                    catch (Exception e)
                    {
                        Debug.Print($"Failed to kill process: {e.Message}");
                    }
                });
        }

        private static async Task InstallEventStoreAsync(Version version)
        {
            if (IsEventStoreInstalled(version))
            {
                Debug.Print($"EventStore v'{version}' is already installed");
                return;
            }

            Debug.Print($"EventStore v{version} not installed, installing it");

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
            Debug.Print($"Extracting '{zipSourcePath}' to '{directoryDestinationPath}'");

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

            Debug.Print($"Downloading '{sourceUri}' to '{destinationPath}'");

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