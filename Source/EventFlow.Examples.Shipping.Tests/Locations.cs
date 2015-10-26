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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;

namespace EventFlow.Examples.Shipping.Tests
{
    public static class Locations
    {
        public static readonly LocationId Hongkong = new LocationId("CNHKG");
        public static readonly LocationId Melbourne = new LocationId("AUMEL");
        public static readonly LocationId Stockholm = new LocationId("SESTO");
        public static readonly LocationId Helsinki = new LocationId("FIHEL");
        public static readonly LocationId Chicago = new LocationId("USCHI");
        public static readonly LocationId Tokyo = new LocationId("JNTKO");
        public static readonly LocationId Hamburg = new LocationId("DEHAM");
        public static readonly LocationId Shanghai = new LocationId("CNSHA");
        public static readonly LocationId Rotterdam = new LocationId("NLRTM");
        public static readonly LocationId Gothenburg = new LocationId("SEGOT");
        public static readonly LocationId Hangzou = new LocationId("CNHGH");
        public static readonly LocationId NewYork = new LocationId("USNYC");
        public static readonly LocationId Dallas = new LocationId("USDAL");

        public static IEnumerable<Location> GetLocations()
        {
            var fieldInfos = typeof (Locations).GetFields(BindingFlags.Public | BindingFlags.Static);
            return fieldInfos.Select(fi => new Location((LocationId) fi.GetValue(null), fi.Name));
        }
    }
}