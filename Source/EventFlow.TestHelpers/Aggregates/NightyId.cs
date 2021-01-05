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
using System.Text.RegularExpressions;
using EventFlow.Core;

namespace EventFlow.TestHelpers.Aggregates
{
    public class NightyId : IIdentity
    {
        public NightyId(string value)
        {
            // This identity class has purposefully been tailored to mimic
            // the ThingyId using the IIdentity-interface instead.
            // The purpose is to produce identity values that are identical
            // between the two different aggregate types, in order to verify
            // that there will be no conflicts between identity spaces.
            // An aggregates should be allowed to have the same identity
            // _value_ as another aggregate of a different type without
            // any collisions occurring.
            var nameReplace = new Regex("Id$");
            var prefix = nameReplace.Replace(nameof(ThingyId), string.Empty).ToLowerInvariant() + "-";
            Value = $"{prefix}{value}";
        }

        public string Value { get; }

        public static NightyId New
            => new NightyId(Guid.NewGuid().ToString("D"));

        public static NightyId With(Guid value)
            => new NightyId(value.ToString("D"));
    }
}