// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;

namespace EventFlow.ReadStores
{
    public class ReadModelPopulatorTracker : IReadModelPopulatorTracker
    {
        public bool PopulationInProgress { get; private set; }
        public long EventsLoaded { get; private set; }
        public GlobalPosition LoadedPosition { get; private set; }
        public long EventsProcessed { get; private set; }

        public ReadModelPopulatorTracker()
        {
            PopulationInProgress = false;
            EventsLoaded = 0;
            LoadedPosition = GlobalPosition.Start;
            EventsProcessed = 0;
        }
        
        public Task PopulationStarted(CancellationToken token)
        {
            EventsLoaded = 0;
            LoadedPosition = GlobalPosition.Start;
            EventsProcessed = 0;
            PopulationInProgress = true;

            return Task.CompletedTask;
        }
        public Task PopulationEnded(CancellationToken token)
        {
            PopulationInProgress = false;

            return Task.CompletedTask;
        }

        public Task NewEventsLoaded(GlobalPosition position, long newEventsLoaded, CancellationToken token)
        {
            LoadedPosition = position;
            EventsLoaded += newEventsLoaded;

            return Task.CompletedTask;
        }

        public Task NewEventsProcessed(long newEventsProcessed, CancellationToken token)
        {
            EventsProcessed += newEventsProcessed;

            return Task.CompletedTask;
        }
    }
}