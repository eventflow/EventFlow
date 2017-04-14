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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;

namespace EventFlow.Core
{
    public class FileSystem : IFileSystem
    {
        private readonly ILog _log;

        public FileSystem(
            ILog log)
        {
            _log = log;
        }

        public Task<bool> FileExistAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(File.Exists(filePath));
        }

        public Task DeleteDirectoryAsync(
            string directoryPath,
            bool recursive,
            CancellationToken cancellationToken)
        {
            if (!Directory.Exists(directoryPath))
            {
                return Task.FromResult(0);
            }

            _log.Verbose($"Deleting directory with recursive '{recursive}': {directoryPath}");

            // No async handles for deleting directories, but it would be nice!
            return Task.Run(() => Directory.Delete(directoryPath, recursive), cancellationToken);
        }

        public async Task<Stream> CreateAsync(
            string filePath,
            bool allowOverwrite,
            CancellationToken cancellationToken)
        {
            if (!allowOverwrite)
            {
                var exists = await FileExistAsync(filePath, cancellationToken).ConfigureAwait(false);
                if (exists)
                {
                    throw new ArgumentException($"File '{filePath}' already exist!", nameof(filePath));
                }
            }

            _log.Verbose($"Creating file: {filePath}");

            var fileMode = allowOverwrite
                ? FileMode.Create
                : FileMode.CreateNew;

            return new FileStream(
                filePath,
                fileMode,
                FileAccess.Write,
                FileShare.None,
                4096,
                true);
        }

        public async Task<Stream> ReadAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            var exists = await FileExistAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                throw new ArgumentException($"File '{filePath}' does not exist!", nameof(filePath));
            }

            _log.Verbose($"Reading file: {filePath}");

            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                true);
        }
    }
}