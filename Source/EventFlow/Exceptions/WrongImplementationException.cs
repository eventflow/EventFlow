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
using System.Collections.Generic;

namespace EventFlow.Exceptions
{
    public enum HelpLinkType
    {
        Aggregates,
        MetadataProviders,
    }

    public class WrongImplementationException : Exception
    {
        private static readonly Dictionary<HelpLinkType, string> HelpLinks = new Dictionary<HelpLinkType, string>
            {
                {HelpLinkType.Aggregates, "https://github.com/rasmus/EventFlow/blob/master/Documentation/Aggregates.md"},
                {HelpLinkType.MetadataProviders, "https://github.com/rasmus/EventFlow/blob/master/Documentation/MetadataProviders.md"},
            };

        public override string HelpLink => HelpLinks[HelpLinkType];
        public HelpLinkType HelpLinkType { get; } 

        private WrongImplementationException(HelpLinkType helpLinkType, string message)
            : base(string.Format("{0}{1}Help link:{2}", message, Environment.NewLine, HelpLinks[helpLinkType]))
        {
            HelpLinkType = helpLinkType;
        }

        public static WrongImplementationException With(HelpLinkType helpLinkType, string message)
        {
            return new WrongImplementationException(helpLinkType, message);
        }

        public static WrongImplementationException With(HelpLinkType helpLinkType, string format, params object[] args)
        {
            return new WrongImplementationException(helpLinkType, string.Format(format, args));
        }
    }
}
