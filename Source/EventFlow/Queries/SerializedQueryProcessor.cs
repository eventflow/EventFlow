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
using Microsoft.Extensions.Logging;

namespace EventFlow.Queries
{
    public interface ISerializedQueryProcessor
    {
        Task<object> ProcessAsync(string name, int version, string json, CancellationToken cancellationToken = default);
    }

    public class SerializedQueryProcessor : ISerializedQueryProcessor
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<SerializedQueryProcessor> _logger;
        private readonly IQueryDefinitionService _queryDefinitionService;
        private readonly IQueryProcessor _queryProcessor;
        public SerializedQueryProcessor(
            IJsonSerializer jsonSerializer,
            ILogger<SerializedQueryProcessor> logger,
            IQueryDefinitionService queryDefinitionService,
            IQueryProcessor queryProcessor)
        {
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryDefinitionService = queryDefinitionService ?? throw new ArgumentNullException(nameof(queryDefinitionService));
            _queryProcessor = queryProcessor ?? throw new ArgumentNullException(nameof(queryProcessor));
        }

        public async Task<object> ProcessAsync(string name, int version, string json, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (string.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            _logger.LogTrace(
                "Executing serialized query {QueryName} v{Version}",
                name,
                version);
            if (!_queryDefinitionService.TryGetDefinition(name, version, out var queryDefinition))
            {
                throw new ArgumentException($"No query definition found for query '{name}' v{version}");

            }
            IQuery query;
            try
            {
                query = (IQuery)_jsonSerializer.Deserialize(json, queryDefinition.Type);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to deserialize query '{name}' v{version}: {e.Message}", e);

            }

            var result = await _queryProcessor.ProcessAsync(query, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}