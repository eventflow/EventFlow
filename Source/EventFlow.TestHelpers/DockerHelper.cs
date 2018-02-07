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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using EventFlow.Core;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public static class DockerHelper
    {
        private static readonly DockerClient DockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"))
            .CreateClient();

        public static async Task<IDisposable> StartContainerAsync(
            string image,
            IReadOnlyCollection<int> ports = null,
            IReadOnlyDictionary<string, string> environment = null)
        {
            var env = environment
                ?.OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}")
                .ToList();
            LogHelper.Log.Information($"Starting container with image '{image}'");

            LogHelper.Log.Information($"Pulling image {image}");
            await DockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                    {
                        FromImage = image,
                    },
                null,
                new Progress<JSONMessage>(m => LogHelper.Log.Verbose($"{m.ProgressMessage} ({m.ID})")))
                .ConfigureAwait(false);

            var createContainerResponse = await DockerClient.Containers.CreateContainerAsync(
                new CreateContainerParameters(
                    new Config
                        {
                            Image = image,
                            Env = env,
                            ExposedPorts = ports?
                                .ToDictionary(p => $"{p}/tcp", p => new EmptyStruct()),
                        })
                    {
                        HostConfig = new HostConfig
                        {
                            PortBindings = ports?.ToDictionary(
                                p => $"{p}/tcp",
                                p => (IList<PortBinding>) new List<PortBinding> { new PortBinding{ HostPort = p.ToString()} })
                        }
                    })
                .ConfigureAwait(false);
            LogHelper.Log.Information($"Successfully created container '{createContainerResponse.ID}'");

            await DockerClient.Containers.StartContainerAsync(
                createContainerResponse.ID,
                new ContainerStartParameters())
                .ConfigureAwait(false);
            LogHelper.Log.Information($"Successfully started container '{createContainerResponse.ID}'");
            
            return new DisposableAction(() =>  StopContainer(createContainerResponse.ID));
        }

        private static void StopContainer(string id)
        {
            try
            {
                DockerClient.Containers.StopContainerAsync(
                    id,
                    new ContainerStopParameters
                    {
                        WaitBeforeKillSeconds = 5
                    }).Wait();
                LogHelper.Log.Information($"Stopped container {id}");

                DockerClient.Containers.RemoveContainerAsync(
                    id,
                    new ContainerRemoveParameters
                    {
                        Force = true
                    }).Wait();
                LogHelper.Log.Information($"Removed container {id}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Test, Explicit]
        public static async Task Test()
        {
            using (await StartContainerAsync(
                "rabbitmq:3.6-management-alpine",
                new []{ 15672 }))
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
}
