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

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventFlow.AspNetCore.Extensions;
using EventFlow.AspNetCore.Middlewares;
using EventFlow.Configuration;
using EventFlow.Configuration.Serialization;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace EventFlow.AspNetCore.Tests.IntegrationTests.Site
{
    public abstract class SiteTestsBase : Test
    {
        private HttpClient _client;
        private IJsonOptions _jsonOptions;
        private ConsoleLog _log;
        private TestServer _server;
        protected IEventStore EventStore;

        [SetUp]
        public void SetUp()
        {
            IWebHostBuilder builder = new WebHostBuilder().UseStartup(GetType());
            _server = new TestServer(builder);
            _client = _server.CreateClient();
            _log = new ConsoleLog();
            _jsonOptions = GetService<IJsonOptions>();
            EventStore = GetService<IEventStore>();
        }

        [TearDown]
        public void TearDown()
        {
            _server.Dispose();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();

            services.AddLogging(logging => logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            services
                .AddEventFlow(o => o
                    .AddDefaults(EventFlowTestHelpers.Assembly)
                    .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                    .ConfigureJson(j => j
                        .AddSingleValueObjects())
                    .AddAspNetCore(ConfigureAspNetCore));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<TestAuthenticationMiddleware>();
            app.UseMiddleware<CommandPublishMiddleware>();

            app.UseRouting();

            app.UseEndpoints(e => e.MapDefaultControllerRoute());
        }

        protected abstract void ConfigureAspNetCore(AspNetCoreEventFlowOptions options);

        private T GetService<T>()
        {
            return _server.Host.Services.GetRequiredService<T>();
        }

        protected async Task<string> GetAsync(string url)
        {
            HttpResponseMessage response = await _client.GetAsync(url);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);


            _log.Information(
                "Received a '{0}' from '{1}' with this content: {2}",
                response.StatusCode,
                response.RequestMessage.RequestUri,
                content);

            response.EnsureSuccessStatusCode();

            return content;
        }

        protected async Task<string> PostAsync(string url, object obj)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            _jsonOptions.Apply(settings);
            var json = JsonConvert.SerializeObject(obj, settings);
            StringContent stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync(url, stringContent);

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            _log.Information(
                "Received a '{0}' from '{1}' with this content: {2}",
                response.StatusCode,
                response.RequestMessage.RequestUri,
                content);

            response.EnsureSuccessStatusCode();

            return content;
        }
    }
}
