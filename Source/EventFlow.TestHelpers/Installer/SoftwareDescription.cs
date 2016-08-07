// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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

using System;

namespace EventFlow.TestHelpers.Installer
{
    public class SoftwareDescription
    {
        public static SoftwareDescription Create(
            string shortName,
            Version version,
            Uri downloadUri)
        {
            return new SoftwareDescription(shortName, version, downloadUri);
        }

        public static SoftwareDescription Create(
            string shortName,
            Version version,
            string downloadUri)
        {
            return Create(shortName, version, new Uri(downloadUri, UriKind.Absolute));
        }

        public string ShortName { get; }
        public Version Version { get; }
        public Uri DownloadUri { get; }

        private SoftwareDescription(
            string shortName,
            Version version,
            Uri downloadUri)
        {
            if (string.IsNullOrEmpty(shortName)) throw new ArgumentNullException(nameof(shortName));
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (downloadUri == null) throw new ArgumentNullException(nameof(downloadUri));
            if (!downloadUri.IsAbsoluteUri) throw new ArgumentException($"'{downloadUri.OriginalString}' is not absolute");

            ShortName = shortName;
            Version = version;
            DownloadUri = downloadUri;
        }

        public override string ToString()
        {
            return $"{ShortName} v{Version}";
        }
    }
}