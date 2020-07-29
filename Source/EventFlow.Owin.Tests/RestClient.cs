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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventFlow.Logs;
using Newtonsoft.Json;

namespace EventFlow.Owin.Tests
{
    public class RestClient
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ConsoleLog _log;

        public RestClient()
        {
            _log = new ConsoleLog();
        }

        public Task<string> GetAsync(Uri uri)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Get, uri));
        }

        public Task<string> PostObjectAsync<T>(Uri uri, T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = stringContent,
                };

            return SendAsync(httpRequestMessage);
        }

        private async Task<string> SendAsync(HttpRequestMessage httpRequestMessage)
        {
            using (httpRequestMessage)
            using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false))
            {
                var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                _log.Information(
                    "Received a '{0}' from '{1}' with this content: {2}",
                    httpResponseMessage.StatusCode,
                    httpRequestMessage.RequestUri,
                    content);

                httpResponseMessage.EnsureSuccessStatusCode();

                return content;
            }
        }
    }
}