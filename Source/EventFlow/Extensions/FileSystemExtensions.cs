// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;

namespace EventFlow.Extensions
{
    public static class FileSystemExtensions
    {
        public static async Task<string> ReadAllTextAsync(
            this IFileSystem fileSystem,
            string filePath,
            CancellationToken cancellationToken)
        {
            var stringBuilder = new StringBuilder();

            using (var stream = await fileSystem.ReadAsync(filePath, cancellationToken).ConfigureAwait(false))
            {
                var buffer = new byte[0x4000];
                int numRead;

                while ((numRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, numRead);
                    stringBuilder.Append(text);
                }
            }

            return stringBuilder.ToString();
        }

        public static async Task WriteAllTextAsync(
            this IFileSystem fileSystem,
            string filePath,
            string text,
            bool allowOverwrite,
            CancellationToken cancellationToken)
        {
            using (var stream = await fileSystem.CreateAsync(filePath, allowOverwrite, cancellationToken).ConfigureAwait(false))
            {
                var encodedText = Encoding.UTF8.GetBytes(text);

                await stream.WriteAsync(encodedText, 0, encodedText.Length, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}