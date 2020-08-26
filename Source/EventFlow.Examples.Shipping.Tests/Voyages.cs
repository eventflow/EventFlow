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

using System.Collections.Generic;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.ValueObjects;
using FluentAssertions.Extensions;

namespace EventFlow.Examples.Shipping.Tests
{
    public static class Voyages
    {
        // 0100S: Hongkong - Hangzou - Tokyo - Melbourne - New York (by ship)
        public static VoyageId HongkongToNewYorkId { get; } = new VoyageId("0100S");
        public static Schedule HongkongToNewYorkSchedule { get; } = new ScheduleBuilder(Locations.Hongkong)
            .Add(Locations.Hangzou, 1.October(2008).At(12, 00), 3.October(2008).At(14, 30))
            .Add(Locations.Tokyo, 3.October(2008).At(21, 00), 6.October(2008).At(06, 15))
            .Add(Locations.Melbourne, 6.October(2008).At(11, 00), 12.October(2008).At(11, 30))
            .Add(Locations.NewYork, 14.October(2008).At(12, 00), 23.October(2008).At(23, 10))
            .Build();

        // 0200T: New York - Chicago - Dallas (by train)
        public static VoyageId NewYorkToDallasId { get; } = new VoyageId("0200T");
        public static Schedule NewYorkToDallasSchedule { get; } = new ScheduleBuilder(Locations.NewYork)
            .Add(Locations.Chicago, 24.October(2008).At(07, 00), 24.October(2008).At(17, 45))
            .Add(Locations.Dallas, 24.October(2008).At(21, 25), 25.October(2008).At(19, 30))
            .Build();

        // 0300A: Dallas - Hamburg - Stockholm - Helsinki (by airplane)
        public static VoyageId DallasToHelsinkiId { get; } = new VoyageId("0300A");
        public static Schedule DallasToHelsinkiSchedule { get; } = new ScheduleBuilder(Locations.Dallas)
            .Add(Locations.Hamburg, 29.October(2008).At(03, 30), 31.October(2008).At(14, 00))
            .Add(Locations.Stockholm, 1.November(2008).At(15, 20), 1.November(2008).At(18, 40))
            .Add(Locations.Helsinki, 2.November(2008).At(09, 00), 2.November(2008).At(11, 15))
            .Build();

        // 0301S: Dallas - Helsinki (by ship)
        public static VoyageId DallasToHelsinkiAltId { get; } = new VoyageId("0301S");
        public static Schedule DallasToHelsinkiAltSchedule { get; } = new ScheduleBuilder(Locations.Dallas)
            .Add(Locations.Helsinki, 29.October(2008).At(03, 00), 5.November(2008).At(15, 45))
            .Build();

        // 0400S: Helsinki - Rotterdam - Shanghai - Hongkong (by ship)
        public static VoyageId HelsinkiToHongkongId { get; } = new VoyageId("0400S");
        public static Schedule HelsinkiToHongkongSchedule { get; } = new ScheduleBuilder(Locations.Helsinki)
            .Add(Locations.Rotterdam, 4.November(2008).At(05, 50), 6.November(2008).At(14, 10))
            .Add(Locations.Shanghai, 10.November(2008).At(21, 45), 22.November(2008).At(16, 40))
            .Add(Locations.Hongkong, 24.November(2008).At(07, 00), 28.November(2008).At(13, 37))
            .Build();

        public static IEnumerable<Voyage> GetVoyages()
        {
            yield return CreateVoyage(HongkongToNewYorkId, HongkongToNewYorkSchedule);
            yield return CreateVoyage(NewYorkToDallasId, NewYorkToDallasSchedule);
            yield return CreateVoyage(DallasToHelsinkiId, DallasToHelsinkiSchedule);
            yield return CreateVoyage(DallasToHelsinkiAltId, DallasToHelsinkiAltSchedule);
            yield return CreateVoyage(HelsinkiToHongkongId, HelsinkiToHongkongSchedule);
        }

        private static Voyage CreateVoyage(VoyageId voyageId, Schedule schedule)
        {
            return new Voyage(voyageId, schedule);
        }
    }
}