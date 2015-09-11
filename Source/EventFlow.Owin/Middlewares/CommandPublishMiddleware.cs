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
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Logs;
using Microsoft.Owin;

namespace EventFlow.Owin.Middlewares
{
    public class CommandPublishMiddleware : OwinMiddleware
    {
        private static readonly Regex CommandPath = new Regex(
            @"/*commands/(?<name>[a-z]+)/(?<version>\d+)/{0,1}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILog _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ICommandDefinitionService _commandDefinitionService;
        private readonly ICommandBus _commandBus;

        public CommandPublishMiddleware(
            OwinMiddleware next,
            ILog log,
            IJsonSerializer jsonSerializer,
            ICommandDefinitionService commandDefinitionService,
            ICommandBus commandBus)
            : base(next)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _commandDefinitionService = commandDefinitionService;
            _commandBus = commandBus;
        }

        public override async Task Invoke(IOwinContext context)
        {
            var path = context.Request.Path;
            if (context.Request.Method == "POST" && path.HasValue)
            {
                var match = CommandPath.Match(path.Value);
                if (match.Success)
                {
                    await PublishCommandAsync(
                        match.Groups["name"].Value, 
                        int.Parse(match.Groups["version"].Value),
                        context)
                        .ConfigureAwait(false);
                    return;
                }
            }

            await Next.Invoke(context).ConfigureAwait(false);
        }

        private async Task PublishCommandAsync(string name, int version, IOwinContext context)
        {
            _log.Verbose($"Publishing command '{name}' v{version} from OWIN middleware");

            string requestJson;
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                requestJson = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(requestJson))
            {
                _log.Debug($"There was no body for the publish request of command {name} v{version}");
                await WriteErrorAsync("No request body",
                    HttpStatusCode.BadRequest,
                    context)
                    .ConfigureAwait(false);
                return;
            }

            CommandDefinition commandDefinition;
            if (!_commandDefinitionService.TryGetCommandDefinition(name, version, out commandDefinition))
            {
                _log.Debug($"No command definition found for command '{name}' v{version}");
                await WriteErrorAsync($"No command named '{name}' with version '{version}'",
                    HttpStatusCode.NotFound,
                    context)
                    .ConfigureAwait(false);
                return;
            }

            ICommand command;
            try
            {
                command = (ICommand)_jsonSerializer.Deserialize(requestJson, commandDefinition.Type);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Failed to deserilize command '{name}' v{version}: {e.Message}");
                return;
            }

            try
            {
                var sourceId = await command.PublishAsync(_commandBus, CancellationToken.None).ConfigureAwait(false);
                _log.Verbose($"Sucessfully published command '{name}' v{version} with source ID '{command.GetSourceId().Value}' from OWIN middleware");
                await WriteAsync(
                    new
                        {
                            SourceId = sourceId.Value,
                        },
                    HttpStatusCode.OK,
                    context)
                    .ConfigureAwait(false);
            }
            catch (DomainError e)
            {
                await WriteErrorAsync(e.Message, HttpStatusCode.BadRequest, context).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Error(e, $"Unexpected exception when executing '{name}' v{version}");
                await WriteErrorAsync("Internal server error!", HttpStatusCode.InternalServerError, context).ConfigureAwait(false);
            }
        }

        private async Task WriteAsync(object obj, HttpStatusCode statusCode, IOwinContext owinContext)
        {
            var json = _jsonSerializer.Serialize(obj);
            await owinContext.Response.WriteAsync(json).ConfigureAwait(false);
            owinContext.Response.StatusCode = (int) statusCode;
        }

        private Task WriteErrorAsync(string errorMessage, HttpStatusCode statusCode, IOwinContext owinContext)
        {
            return WriteAsync(
                new
                    {
                        ErrorMessage = errorMessage,
                    },
                statusCode,
                owinContext);
        }
    }
}
