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
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventFlow.TestHelpers
{
    public static class HttpHelper
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task<string> GetAsync(Uri uri)
        {
            LogHelper.Log.Information($"GET {uri}");

            using (var httpResponseMessage = await HttpClient.GetAsync(uri).ConfigureAwait(false))
            {
                var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (content.Length < 1024)
                {
                    LogHelper.Log.Information($"RESPONSE FROM {uri}{Environment.NewLine}{content}");
                }

                return content;
            }
        }

        public static async Task DeleteAsync(Uri uri)
        {
            LogHelper.Log.Information($"DELETE {uri}");

            using (var httpResponseMessage = await HttpClient.DeleteAsync(uri).ConfigureAwait(false))
            {
                var content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (content.Length < 1024)
                {
                    LogHelper.Log.Information($"RESPONSE FROM {uri}{Environment.NewLine}{content}");
                }

                httpResponseMessage.EnsureSuccessStatusCode();
            }
        }

        public static async Task<T> GetAsAsync<T>(Uri uri)
        {
            var json = await GetAsync(uri).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}