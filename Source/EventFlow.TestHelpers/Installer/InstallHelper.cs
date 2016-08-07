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
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventFlow.TestHelpers.Installer
{
    public class InstallHelper
    {
        public static async Task<InstalledSoftware> InstallAsync(SoftwareDescription softwareDescription)
        {
            var installPath = GetInstallPath(softwareDescription);
            var isInstalled = Directory.Exists(installPath);

            if (isInstalled)
            {
                Console.WriteLine($"{softwareDescription}' is already installed");
                return new InstalledSoftware(softwareDescription, installPath);
            }

            Console.WriteLine($"{softwareDescription} not installed, installing it");

            var tempDownload = Path.Combine(
                Path.GetTempPath(),
                $"{softwareDescription.ShortName}-v{softwareDescription.Version}-{Guid.NewGuid().ToString("N")}.zip");

            try
            {
                await DownloadFileAsync(softwareDescription.DownloadUri, tempDownload).ConfigureAwait(false);
                ExtractZipFile(tempDownload, installPath);
                return new InstalledSoftware(softwareDescription, installPath);
            }
            finally
            {
                if (File.Exists(tempDownload))
                {
                    File.Delete(tempDownload);
                }
            }
        }

        private static void ExtractZipFile(string zipSourcePath, string directoryDestinationPath)
        {
            Console.WriteLine($"Extracting '{zipSourcePath}' to '{directoryDestinationPath}'");

            if (!Directory.Exists(directoryDestinationPath))
            {
                Directory.CreateDirectory(directoryDestinationPath);
            }

            ZipFile.ExtractToDirectory(zipSourcePath, directoryDestinationPath);
        }

        private static string GetInstallPath(SoftwareDescription softwareDescription)
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"eventflow-{softwareDescription.ShortName}-v{softwareDescription.Version}");
        }

        private static async Task DownloadFileAsync(Uri sourceUri, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                throw new ArgumentException($"File '{destinationPath}' already exists");
            }

            Console.WriteLine($"Downloading '{sourceUri}' to '{destinationPath}'");

            using (var httpClient = new HttpClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, sourceUri))
            using (var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false))
            {
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to download '{sourceUri}' due to '{httpResponseMessage.StatusCode}'");
                }

                using (var sourceStream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var destinationStream = new FileStream(destinationPath, FileMode.CreateNew))
                {
                    await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                }
            }
        }
    }
}