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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using Newtonsoft.Json;

namespace EventFlow.EventArchives.Formatters.GZippedJson
{
    public class GZippedJsonEventArchiveStreamFormatter : IEventArchiveStreamFormatter
    {
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer
        {
            Formatting = Formatting.None
        };

        public async Task StreamEventsAsync(
            Stream stream,
            ICommittedDomainEventStream committedDomainEventStream,
            CancellationToken cancellationToken)
        {
            using (var gZipStream = new GZipStream(stream, CompressionLevel.Fastest, true))
            using (var streamWriter = new StreamWriter(gZipStream, Encoding.UTF8, 1024, true))
            using (var jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonTextWriter.WriteStartArray();

                IReadOnlyCollection<ICommittedDomainEvent> committedDomainEvents;
                while ((committedDomainEvents = await committedDomainEventStream.ReadAsync(cancellationToken).ConfigureAwait(false)).Any())
                    foreach (var committedDomainEvent in committedDomainEvents)
                    {
                        var jsonEvent = new JsonEvent(
                            committedDomainEvent.Data,
                            committedDomainEvent.Metadata);

                        _jsonSerializer.Serialize(
                            jsonTextWriter,
                            jsonEvent);
                    }

                jsonTextWriter.WriteEndArray();
            }
        }

        public class JsonEvent
        {
            public JsonEvent(
                string @event,
                string metadata)
            {
                Event = @event;
                Metadata = metadata;
            }

            [JsonProperty("event")]
            public string Event { get; }

            [JsonProperty("metadata")]
            public string Metadata { get; }
        }
    }
}