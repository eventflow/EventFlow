﻿// The MIT License (MIT)
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Commands;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.AspNetCore.Tests.IntegrationTests.Site
{
	[Category(Categories.Integration)]
	public class SiteTests : Test
	{
		private TestServer _server;
		private HttpClient _client;
		private ConsoleLog _log;

		[SetUp]
		public void SetUp()
		{
			_server = new TestServer(new WebHostBuilder()
				.UseStartup<Startup>());
			_client = _server.CreateClient();
			_log = new ConsoleLog();
		}

		[TearDown]
		public void TearDown()
		{
			_server.Dispose();
		}

		[Test]
		public async Task Ping()
		{
			// Act
			await GetAsync("thingy/ping?id=thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3").ConfigureAwait(false);
			await GetAsync("thingy/ping?id=thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3").ConfigureAwait(false);
		}

		[Test]
		public async Task PublishCommand()
		{
			// Arrange
			var pingCommand = A<ThingyPingCommand>();

			// Act
			await PostAsync("commands/ThingyPing/1", pingCommand).ConfigureAwait(false);
		}

		[Test]
		public void PublishCommand_WithNull_ThrowsException()
		{
			// Arrange + Act
			Action action = () => Task.WaitAll(PostAsync("commands/ThingyPing/1", null));

			action.Should().Throw<HttpRequestException>("because of command is null.");
		}

		private async Task<string> GetAsync(string url)
		{
			// Act
			var response = await _client.GetAsync(url);

			var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			

			_log.Information(
				"Received a '{0}' from '{1}' with this content: {2}",
				response.StatusCode,
				response.RequestMessage.RequestUri,
				content);

			response.EnsureSuccessStatusCode();

			return content;
		}

		private async Task<string> PostAsync(string url, object obj)
		{
			var json = JsonConvert.SerializeObject(obj);
			var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
		
			var response = await _client.PostAsync(url, stringContent);
			
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