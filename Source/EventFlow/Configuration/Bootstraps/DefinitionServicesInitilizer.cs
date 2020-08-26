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
using EventFlow.Commands;
using EventFlow.EventStores;
using EventFlow.Jobs;
using EventFlow.Sagas;
using EventFlow.Snapshots;

namespace EventFlow.Configuration.Bootstraps
{
    public class DefinitionServicesInitilizer : IBootstrap
    {
        private readonly ICommandDefinitionService _commandDefinitionService;
        private readonly IEventDefinitionService _eventDefinitionService;
        private readonly IJobDefinitionService _jobDefinitionService;
        private readonly ISagaDefinitionService _sagaDefinitionService;
        private readonly ISnapshotDefinitionService _snapshotDefinitionService;
        private readonly ILoadedVersionedTypes _loadedVersionedTypes;

        public DefinitionServicesInitilizer(
            ILoadedVersionedTypes loadedVersionedTypes,
            IEventDefinitionService eventDefinitionService,
            ICommandDefinitionService commandDefinitionService,
            IJobDefinitionService jobDefinitionService,
            ISagaDefinitionService sagaDefinitionService,
            ISnapshotDefinitionService snapshotDefinitionService)
        {
            _loadedVersionedTypes = loadedVersionedTypes;
            _eventDefinitionService = eventDefinitionService;
            _commandDefinitionService = commandDefinitionService;
            _jobDefinitionService = jobDefinitionService;
            _sagaDefinitionService = sagaDefinitionService;
            _snapshotDefinitionService = snapshotDefinitionService;
        }

        public Task BootAsync(CancellationToken cancellationToken)
        {
            _commandDefinitionService.Load(_loadedVersionedTypes.Commands);
            _eventDefinitionService.Load(_loadedVersionedTypes.Events);
            _jobDefinitionService.Load(_loadedVersionedTypes.Jobs);
            _sagaDefinitionService.LoadSagas(_loadedVersionedTypes.Sagas);
            _snapshotDefinitionService.Load(_loadedVersionedTypes.SnapshotTypes);

            return Task.FromResult(0);
        }
    }
}