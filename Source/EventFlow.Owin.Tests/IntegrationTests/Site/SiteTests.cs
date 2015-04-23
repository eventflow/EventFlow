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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using NUnit.Framework;

namespace EventFlow.Owin.Tests.IntegrationTests.Site
{
    public class SiteTests
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private IDisposable _site;
        private Uri _uri;

        [SetUp]
        public void SetUp()
        {
            _uri = new Uri("http://localhost:9000");
            _site = WebApp.Start<Startup>(_uri.ToString());
        }

        [TearDown]
        public void TearDown()
        {
            _site.Dispose();
        }

        [Test]
        public async Task Ping()
        {
            // Act
            await GetAsync("testaggregate/ping?id=fancy").ConfigureAwait(false);
            await GetAsync("testaggregate/ping?id=fancy").ConfigureAwait(false);
        }

        private async Task<string> GetAsync(string url)
        {
            var uri = new Uri(_uri, url);

            using (var httpResponseMessage = await HttpClient.GetAsync(uri).ConfigureAwait(false))
            {
                var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                Console.WriteLine("Received a '{0}' from '{1}' with this content: {2}", httpResponseMessage.StatusCode, url, content);
                httpResponseMessage.EnsureSuccessStatusCode();
                Console.WriteLine("Received content from {0} : {1}", url, content);
                return content;
            }
        }
    }
}
