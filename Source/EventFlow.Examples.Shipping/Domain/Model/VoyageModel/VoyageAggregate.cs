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
using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Events;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.ValueObjects;
using EventFlow.Extensions;

namespace EventFlow.Examples.Shipping.Domain.Model.VoyageModel
{
    public class VoyageAggregate : AggregateRoot<VoyageAggregate, VoyageId>
    {
        private readonly VoyageState _state = new VoyageState();

        public VoyageAggregate(VoyageId id) : base(id)
        {
            Register(_state);
        }

        public Schedule Schedule => _state.Schedule;

        public void Create(Schedule schedule)
        {
            Specs.AggregateIsNew.ThrowDomainErrorIfNotStatisfied(this);

            Emit(new VoyageCreatedEvent(schedule));
        }

        public void Delay(TimeSpan delay)
        {
            Specs.AggregateIsCreated.ThrowDomainErrorIfNotStatisfied(this);

            if (delay == TimeSpan.Zero) return;

            var delayedSchedule = Schedule.Delay(delay);

            Emit(new VoyageScheduleUpdatedEvent(delayedSchedule));
        }
    }
}