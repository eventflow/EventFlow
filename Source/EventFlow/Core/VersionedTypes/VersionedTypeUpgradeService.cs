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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Core.VersionedTypes
{
    public abstract class VersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType, TDefinitionService> :
        IVersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
        where TVersionedType : IVersionedType
        where TDefinitionService : IVersionedTypeDefinitionService<TAttribute, TDefinition>
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly TDefinitionService _definitionService;

        protected VersionedTypeUpgradeService(
            ILog log,
            IResolver resolver,
            TDefinitionService definitionService)
        {
            _log = log;
            _resolver = resolver;
            _definitionService = definitionService;
        }

        public async Task<TVersionedType> UpgradeAsync(TVersionedType versionedType, CancellationToken cancellationToken)
        {
            var currentDefinition = _definitionService.GetDefinition(versionedType.GetType());
            var definitionsWithHigherVersion = _definitionService.GetDefinitions(currentDefinition.Name)
                .Where(d => d.Version > currentDefinition.Version)
                .OrderBy(d => d.Version)
                .ToList();

            if (!definitionsWithHigherVersion.Any())
            {
                _log.Verbose(() => $"No need to update '{versionedType.GetType().PrettyPrint()}' as its already the correct version");
                return versionedType;
            }

            _log.Verbose(() => $"Snapshot '{currentDefinition.Name}' v{currentDefinition.Version} needs to be upgraded to v{definitionsWithHigherVersion.Last().Version}");

            foreach (var nextDefinition in definitionsWithHigherVersion)
            {
                versionedType = await UpgradeToVersionAsync(
                    versionedType,
                    currentDefinition,
                    nextDefinition,
                    cancellationToken)
                    .ConfigureAwait(false);
                currentDefinition = nextDefinition;
            }

            return versionedType;
        }

        protected abstract Type CreateUpgraderType(Type fromType, Type toType);

        private async Task<TVersionedType> UpgradeToVersionAsync(
            TVersionedType versionedType,
            TDefinition fromDefinition,
            TDefinition toDefinition,
            CancellationToken cancellationToken)
        {
            _log.Verbose($"Upgrading '{fromDefinition}' to '{toDefinition}'");

            var upgraderType = CreateUpgraderType(fromDefinition.Type, toDefinition.Type);
            var versionedTypeUpgraderType = typeof(IVersionedTypeUpgrader<,>).MakeGenericType(fromDefinition.Type, toDefinition.Type);
            var versionedTypeUpgrader = _resolver.Resolve(upgraderType);

            var methodInfo = versionedTypeUpgraderType.GetTypeInfo().GetMethod("UpgradeAsync");

            var task = (Task) methodInfo.Invoke(versionedTypeUpgrader, new object[] { versionedType, cancellationToken });

            await task.ConfigureAwait(false);

            return ((dynamic) task).Result;
        }
    }
}