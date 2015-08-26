﻿// The MIT License (MIT)
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
using System.Text.RegularExpressions;

namespace EventFlow.Core
{
    public class Label
    {
        private static readonly Regex NameValidator = new Regex(@"^[a-z0-9\-]{3,}$", RegexOptions.Compiled);

        public static Label Named(string name) { return new Label(name.ToLowerInvariant()); }

        public static Label Named(params string[] parts)
        {
            return Named(string.Join("-", parts));
        }

        public string Name { get; private set; }

        private Label(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (!NameValidator.IsMatch(name)) throw new ArgumentException(string.Format(
                "Label '{0}' is not a valid label, it must pass this regex '{1}'",
                name,
                NameValidator));

            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
