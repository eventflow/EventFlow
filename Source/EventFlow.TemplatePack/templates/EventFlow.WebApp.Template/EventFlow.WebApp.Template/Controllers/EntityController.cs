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

using EventFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EventFlow.Queries;
using System.Threading;

namespace EventFlow.WebApp.Template.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EntityController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private readonly ICommandBus _commandBus;
        private readonly IQueryProcessor _queryProcessor;
        private readonly ILogger<EntityController> _logger;

        // The Web API will only accept tokens 1) for users, and 2) having the "access_as_user" scope for this API
        static readonly string[] scopeRequiredByApi = new string[] { "access_as_user" };

        public EntityController(ICommandBus commandBus, IQueryProcessor queryProcessor, ILogger<EntityController> logger)
        {
            _commandBus = commandBus;
            _queryProcessor = queryProcessor;
            _logger = logger;
        }

        [HttpGet("{id}", Name = "Get")]
        public async Task<ActionResult<Domain.EntityReadModel>> Get(Guid id)
        {
            var identity = new Domain.EntityId(id);

            var readModel = await _queryProcessor
                .ProcessAsync(new ReadModelByIdQuery<Domain.EntityReadModel>(identity), CancellationToken.None)
                .ConfigureAwait(false);

            return readModel switch
            {
                null => new NotFoundResult(),
                _ => new OkObjectResult(readModel)
            };
        }

        [HttpPost("{id}/increment", Name = "Increment")]
        public async Task<Domain.EntityReadModel> Increment(Guid id)
        {
            var identity = new Domain.EntityId(id);

            await _commandBus
                .PublishAsync(new Domain.Commands.Increment.Command(identity), CancellationToken.None)
                .ConfigureAwait(false);

            var readModel = await _queryProcessor
                .ProcessAsync(new ReadModelByIdQuery<Domain.EntityReadModel>(identity), CancellationToken.None)
                .ConfigureAwait(false);

            return readModel;
        }

        [HttpPost("{id}/decrement", Name = "Decrement")]
        public async Task<Domain.EntityReadModel> Decrement(Guid id)
        {
            var identity = new Domain.EntityId(id);

            await _commandBus
                .PublishAsync(new Domain.Commands.Decrement.Command(identity), CancellationToken.None)
                .ConfigureAwait(false);

            var readModel = await _queryProcessor
                .ProcessAsync(new ReadModelByIdQuery<Domain.EntityReadModel>(identity), CancellationToken.None)
                .ConfigureAwait(false);

            return readModel;
        }
    }
}
