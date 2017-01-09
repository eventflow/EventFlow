// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
// 

using EventFlow.Aggregates;
using EventFlow.Exceptions;

namespace EventFlow.Tests.Documentation.GettingStarted
{
    /// The aggregate root
    public class ExampleAggrenate :
        AggregateRoot<ExampleAggrenate, ExampleId>,
        IEmit<ExampleEvent>
    {
        private int? _magicNumber;

        public ExampleAggrenate(ExampleId id) : base(id) { }

        // Method invoked by our command
        public void SetMagicNumer(int magicNumber)
        {
            if (_magicNumber.HasValue)
                throw DomainError.With("Magic number already set");

            Emit(new ExampleEvent(magicNumber));
        }

        // We apply the event as part of the event sourcing system. EventFlow
        // provides several different methods for doing this, e.g. state objects,
        // the Apply method is merely the simplest
        public void Apply(ExampleEvent aggregateEvent)
        {
            _magicNumber = aggregateEvent.MagicNumber;
        }
    }
}