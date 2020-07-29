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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.AspNetCore.Tests.IntegrationTests.Site
{
    [Route("thingy")]
    public class ThingyController : Controller
    {
        private readonly ICommandBus _commandBus;

        public ThingyController(
            ICommandBus commandBus)
        {
            _commandBus = commandBus;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping(string id)
        {
            ThingyPingCommand pingCommand = new ThingyPingCommand(ThingyId.With(id), PingId.New);
            await _commandBus.PublishAsync(pingCommand, CancellationToken.None).ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("pingWithModelBinding")]
        public async Task<IActionResult> PingWithModelBinding(ThingyId id)
        {
            ThingyPingCommand pingCommand = new ThingyPingCommand(id, PingId.New);
            await _commandBus.PublishAsync(pingCommand, CancellationToken.None).ConfigureAwait(false);
            return Ok();
        }

        [HttpGet("singlevalue/{value}")]
        public IActionResult SingleValue(TestValue value)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            return Ok(value);
        }

        public class TestValue : SingleValueObject<int>
        {
            public TestValue(int value) : base(value)
            {
            }
        }
    }
}
