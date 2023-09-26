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
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.Commands.Serialization
{
    public class CommandJsonSerializer : ICommandJsonSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ICommandDefinitionService _commandDefinitionService;

        public CommandJsonSerializer(
            IJsonSerializer jsonSerializer,
            ICommandDefinitionService commandDefinitionService)
        {
            _jsonSerializer = jsonSerializer;
            _commandDefinitionService = commandDefinitionService;
        }

        public SerializedCommand Serialize<TAggregate, TIdentity, TExecutionResult>(ICommand<TAggregate, TIdentity, TExecutionResult> applicationCommand)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {

            var commandDefinition = _commandDefinitionService.GetDefinition(applicationCommand.GetType());

            var now = DateTimeOffset.Now;
            var aggregateName = typeof(TAggregate).GetAggregateName().Value;

            var metaDataPairs = new CommandMetadata()
            {
                SourceId = applicationCommand.SourceId,
                CommandName = commandDefinition.Name,
                CommandVersion = commandDefinition.Version,
                Timestamp = now,
                AggregateId = applicationCommand.AggregateId.Value,
                AggregateName = aggregateName
            };

            metaDataPairs.Add(MetadataKeys.TimestampEpoch, now.ToUnixTime().ToString());

            var dataJson = _jsonSerializer.Serialize(applicationCommand);
            var metaJson = _jsonSerializer.Serialize(metaDataPairs);

            return new SerializedCommand(
                metaJson,
                dataJson,
                metaDataPairs);
        }

        public ICommand Deserialize(string json, ICommandMetadata metadata)
        {
            return Deserialize(metadata.CommandName, metadata.CommandVersion, json);
        }

        public ICommand Deserialize(string name, int version, string json)
        {
            var commandDefinition = _commandDefinitionService.GetDefinition(
                name,
                version);

            ICommand command;
            try
            {
                command = (ICommand)_jsonSerializer.Deserialize(json, commandDefinition.Type);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to deserialize command '{name}' v{version}: {e.Message}", e);
            }

            return command;
        }
    }
}
