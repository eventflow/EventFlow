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

using EventStore.Client;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.EventStores.EventStore.Subscriptions
{
    public class EventStoreCheckpointStore : IEventStoreCheckpointStore
    {
        const string CheckpointStreamPrefix = "$$checkpoint-";
        readonly EventStoreClient _client;
        readonly string _streamName;

        public EventStoreCheckpointStore(
            EventStoreClient client,
            string subscriptionName)
        {
            _client = client;
            _streamName = CheckpointStreamPrefix + subscriptionName;
        }

        public async Task<ulong?> GetCheckpoint()
        {
            var result = _client.ReadStreamAsync(Direction.Backwards, _streamName, StreamPosition.End, 1);

            if (await result.ReadState == ReadState.StreamNotFound)
                return null;

            var eventData = await result.FirstAsync();

            if (eventData.Equals(default(ResolvedEvent)))
            {
                await StoreCheckpoint(Position.Start.CommitPosition);
                return null;
            }

            return Deserialize<Checkpoint>(eventData)?.Position;
        }

        public Task StoreCheckpoint(ulong? checkpoint)
        {
            var @event = new Checkpoint { Position = checkpoint };

            var preparedEvent =
                new EventData(
                    Uuid.NewUuid(),
                    "$checkpoint",
                    Serialize(@event)
                );

            return _client.AppendToStreamAsync(
                _streamName,
                StreamState.Any,
                new List<EventData> { preparedEvent }
            );
        }

        class Checkpoint
        {
            public ulong? Position { get; set; }
        }

        private byte[] Serialize(object data) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

        public T Deserialize<T>(ResolvedEvent resolvedEvent)
        {
            var jsonData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
            return JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}