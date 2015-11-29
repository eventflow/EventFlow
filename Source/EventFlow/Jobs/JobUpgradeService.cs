// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core.VersionedTypes;
using EventFlow.Logs;

namespace EventFlow.Jobs
{
    public class JobUpgradeService : VersionedTypeUpgradeService<JobVersionAttribute, JobDefinition, IJobDefinitionService>
    {
        public JobUpgradeService(
            ILog log,
            IJobDefinitionService definitionService)
            : base(log, definitionService)
        {
        }

        public Task<IJob> UpgradeAsync(IJob job, CancellationToken cancellationToken)
        {
            var definition = DefinitionService.GetDefinition(job.GetType());
            var versionedTypes = DefinitionService.GetDefinitions(definition.Name)
                .ToList();
            var versionsUpgrade = versionedTypes
                .Where(d => d.Version > definition.Version)
                .ToList();

            if (!versionsUpgrade.Any())
            {
                Log.Verbose($"Nothing to upgrade for job '{definition}'");
                return Task.FromResult(job);
            }

            Log.Verbose(() => $"Upgrading job '{definition}' through these: {string.Join(", ", versionsUpgrade.Select(d => d.ToString()))}");

            return null;
        }
    }
}