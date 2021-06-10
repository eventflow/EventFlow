// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Provided.Jobs
{
    [JobVersion("PublishCommand", 1)]
    public class PublishCommandJob : IJob
    {
        public PublishCommandJob(
            string data,
            string name,
            int version)
        {
            Data = data;
            Name = name;
            Version = version;
        }

        public string Data { get; }
        public string Name { get; }
        public int Version { get; }

        public Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var commandDefinitionService = serviceProvider.GetRequiredService<ICommandDefinitionService>();
            var jsonSerializer = serviceProvider.GetRequiredService<IJsonSerializer>();
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();

            var commandDefinition = commandDefinitionService.GetDefinition(Name, Version);
            var command = (ICommand) jsonSerializer.Deserialize(Data, commandDefinition.Type);

            return command.PublishAsync(commandBus, cancellationToken);
        }

        public static PublishCommandJob Create(
            ICommand command,
            IServiceProvider serviceProvider)
        {
            var commandDefinitionService = serviceProvider.GetRequiredService<ICommandDefinitionService>();
            var jsonSerializer = serviceProvider.GetRequiredService<IJsonSerializer>();

            return Create(command, commandDefinitionService, jsonSerializer);
        }

        public static PublishCommandJob Create(
            ICommand command,
            ICommandDefinitionService commandDefinitionService,
            IJsonSerializer jsonSerializer)
        {
            var data = jsonSerializer.Serialize(command);
            var commandDefinition = commandDefinitionService.GetDefinition(command.GetType());

            return new PublishCommandJob(
                data,
                commandDefinition.Name,
                commandDefinition.Version);
        }
    }
}