// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.IO;
using System.Linq;
using EventFlow.Core;

namespace EventFlow.EventStores.Files
{
    public class FilesEventLocator : IFilesEventLocator
    {
        private readonly IFilesEventStoreConfiguration _configuration;

        public FilesEventLocator(
            IFilesEventStoreConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetEntityPath(IIdentity id)
        {
            return Path.Combine(
                _configuration.StorePath,
                id.Value);
        }

        public string GetEventPath(IIdentity id, int aggregateSequenceNumber)
        {
            return Path.Combine(
                GetEntityPath(id),
                $"{aggregateSequenceNumber}.json");
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="relativeTo">Contains the directory that defines the start of the relative path.</param>
        /// <param name="path">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public string GetRelativePath(string relativeTo, string path)
        {
#if NETCOREAPP3_1 || NETCOREAPP3_0
            return Path.GetRelativePath(relativeTo, path);
#else
            if (string.IsNullOrEmpty(relativeTo))
                throw new ArgumentNullException(nameof(relativeTo));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (relativeTo.Last() != Path.DirectorySeparatorChar && relativeTo.Last() != Path.AltDirectorySeparatorChar)
                relativeTo += Path.DirectorySeparatorChar;

            var fromUri = new Uri(relativeTo);
            var toUri = new Uri(path);

            if (fromUri.Scheme != toUri.Scheme)
                return path; // path can't be made relative.

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
#endif
        }
    }
}