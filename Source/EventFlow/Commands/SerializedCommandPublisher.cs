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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.Commands
{
    public class SerializedCommandPublisher : ISerializedCommandPublisher
    {
        private readonly ILog _log;
        private readonly ICommandDefinitionService _commandDefinitionService;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ICommandBus _commandBus;

        public SerializedCommandPublisher(
            ILog log,
            ICommandDefinitionService commandDefinitionService,
            IJsonSerializer jsonSerializer,
            ICommandBus commandBus)
        {
            _log = log;
            _commandDefinitionService = commandDefinitionService;
            _jsonSerializer = jsonSerializer;
            _commandBus = commandBus;
        }

        public async Task<ISourceId> PublishSerilizedCommandAsync(
            string name,
            int version,
            string json,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (string.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));

            _log.Verbose($"Executing serialized command '{name}' v{version}");

            CommandDefinition commandDefinition;
            if (!_commandDefinitionService.TryGetDefinition(name, version, out commandDefinition))
            {
                throw new ArgumentException($"No command definition found for command '{name}' v{version}");
            }

            ICommand command;
            try
            {
                command = (ICommand)_jsonSerializer.Deserialize(json, commandDefinition.Type);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to deserialize command '{name}' v{version}: {e.Message}", e);
            }

            await command.PublishAsync(_commandBus, CancellationToken.None).ConfigureAwait(false);
            return command.GetSourceId();
        }
    }
}
