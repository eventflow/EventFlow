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
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Logs;
using Microsoft.AspNetCore.Http;

namespace EventFlow.Aspnetcore.Middlewares
{
    public class CommandPublishMiddleware
    {
        private static readonly Regex CommandPath = new Regex(
            @"/*commands/(?<name>[a-z]+)/(?<version>\d+)/{0,1}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly RequestDelegate _next;
        private readonly ILog _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISerializedCommandPublisher _serializedCommandPublisher;

        public CommandPublishMiddleware(
            RequestDelegate next,
            ILog log,
            IJsonSerializer jsonSerializer,
            ISerializedCommandPublisher serializedCommandPublisher)
        {
            _next = next;
            _log = log;
            _jsonSerializer = jsonSerializer;
            _serializedCommandPublisher = serializedCommandPublisher;
        }

        public async Task Invoke(HttpContext context)
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

            await _next.Invoke(context);
        }

        private async Task PublishCommandAsync(string name, int version, HttpContext context)
        {
            _log.Verbose($"Publishing command '{name}' v{version} from OWIN middleware");

            string requestJson;
            using (var streamReader = new StreamReader(context.Request.Body))
            {
                requestJson = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }

            try
            {
                var sourceId = await _serializedCommandPublisher.PublishSerilizedCommandAsync(
                    name,
                    version,
                    requestJson,
                    CancellationToken.None)
                    .ConfigureAwait(false);
                await WriteAsync(
                    new
                        {
                            SourceId = sourceId.Value,
                        },
                    HttpStatusCode.OK,
                    context)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException e)
            {
                _log.Debug(e, $"Failed to publish serilized command '{name}' v{version} due to: {e.Message}");
                await WriteErrorAsync(e.Message, HttpStatusCode.BadRequest, context).ConfigureAwait(false);
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

        private async Task WriteAsync(object obj, HttpStatusCode statusCode, HttpContext context)
        {
            var json = _jsonSerializer.Serialize(obj);
            await context.Response.WriteAsync(json).ConfigureAwait(false);
            context.Response.StatusCode = (int) statusCode;
        }

        private Task WriteErrorAsync(string errorMessage, HttpStatusCode statusCode, HttpContext context)
        {
            return WriteAsync(
                new
                    {
                        ErrorMessage = errorMessage,
                    },
                statusCode,
                context);
        }
    }
}