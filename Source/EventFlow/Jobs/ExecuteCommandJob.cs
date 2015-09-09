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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;

namespace EventFlow.Jobs
{
    public class ExecuteCommandJob : IJob
    {
        public static ExecuteCommandJob Create(ICommand command, IJsonSerializer jsonSerializer)
        {
            var serializedCommand = jsonSerializer.Serialize(command);

            var commandType = command.GetType();

            return new ExecuteCommandJob(
                serializedCommand,
                $"{commandType.Namespace}.{commandType.Name}, {commandType.Assembly.GetName().Name}");
        }

        public string SerializedCommand { get; }
        public string CommandType { get; }

        public ExecuteCommandJob(
            string serializedCommand,
            string commandType)
        {
            SerializedCommand = serializedCommand;
            CommandType = commandType;
        }

        // TODO: Move to "job handler"
        public Task ExecuteAsync(IResolver resolver)
        {
            var commandType = Type.GetType(CommandType, true);
            var jsonSerializer = resolver.Resolve<IJsonSerializer>();
            var command = (ICommand) jsonSerializer.Deserialize(SerializedCommand, commandType);

            var commandBus = resolver.Resolve<ICommandBus>();
            return command.PublishAsync(commandBus, CancellationToken.None);
        }
    }
}
