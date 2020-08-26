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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Core.VersionedTypes
{
    public abstract class VersionedTypeDefinitionService<TTypeCheck, TAttribute, TDefinition> : IVersionedTypeDefinitionService<TAttribute, TDefinition>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Regex NameRegex = new Regex(
            @"^(Old){0,1}(?<name>[\p{L}\p{Nd}]+?)(V(?<version>[0-9]+)){0,1}$",
            RegexOptions.Compiled);

        private readonly object _syncRoot = new object();
        private readonly ILog _log;
        private readonly ConcurrentDictionary<Type, List<TDefinition>> _definitionsByType = new ConcurrentDictionary<Type, List<TDefinition>>();
        private readonly ConcurrentDictionary<string, Dictionary<int, TDefinition>> _definitionByNameAndVersion = new ConcurrentDictionary<string, Dictionary<int, TDefinition>>(); 

        protected VersionedTypeDefinitionService(
            ILog log)
        {
            _log = log;
        }

        public void Load(params Type[] types)
        {
            Load((IReadOnlyCollection<Type>) types);
        }

        public void Load(IReadOnlyCollection<Type> types)
        {
            if (types == null)
            {
                return;
            }

            var invalidTypes = types
                .Where(t => !typeof(TTypeCheck).GetTypeInfo().IsAssignableFrom(t))
                .ToList();
            if (invalidTypes.Any())
            {
                throw new ArgumentException($"The following types are not of type '{typeof(TTypeCheck).PrettyPrint()}': {string.Join(", ", invalidTypes.Select(t => t.PrettyPrint()))}");
            }

            lock (_syncRoot)
            {
                var definitions = types
                    .Distinct()
                    .Where(t => !_definitionsByType.ContainsKey(t))
                    .SelectMany(CreateDefinitions)
                    .ToList();
                if (!definitions.Any())
                {
                    return;
                }

                _log.Verbose(() =>
                    {
                        var assemblies = definitions
                            .Select(d => d.Type.GetTypeInfo().Assembly.GetName().Name)
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
                        return string.Format(
                            "Added {0} versioned types to '{1}' from these assemblies: {2}",
                            definitions.Count,
                            GetType().PrettyPrint(),
                            string.Join(", ", assemblies));
                    });

                foreach (var definition in definitions)
                {
                    var typeDefinitions = _definitionsByType.GetOrAdd(
                        definition.Type,
                        _ => new List<TDefinition>());
                    typeDefinitions.Add(definition);

                    if (!_definitionByNameAndVersion.TryGetValue(definition.Name, out var versions))
                    {
                        versions = new Dictionary<int, TDefinition>();
                        _definitionByNameAndVersion.TryAdd(definition.Name, versions);
                    }

                    if (versions.ContainsKey(definition.Version))
                    {
                        _log.Information(
                            "Already loaded versioned type '{0}' v{1}, skipping it",
                            definition.Name,
                            definition.Version);
                        continue;
                    }

                    versions.Add(definition.Version, definition);
                }
            }
        }

        public IEnumerable<TDefinition> GetDefinitions(string name)
        {
            return _definitionByNameAndVersion.TryGetValue(name, out var versions)
                ? versions.Values.OrderBy(d => d.Version)
                : Enumerable.Empty<TDefinition>();
        }

        public IEnumerable<TDefinition> GetAllDefinitions()
        {
            return _definitionByNameAndVersion.SelectMany(kv => kv.Value.Values);
        } 

        public bool TryGetDefinition(string name, int version, out TDefinition definition)
        {
            if (_definitionByNameAndVersion.TryGetValue(name, out var versions))
            {
                return versions.TryGetValue(version, out definition);
            }

            definition = null;

            return false;
        }

        public TDefinition GetDefinition(string name, int version)
        {
            if (!TryGetDefinition(name, version, out var definition))
            {
                throw new ArgumentException($"No versioned type definition for '{name}' with version {version} in '{GetType().PrettyPrint()}'");
            }

            return definition;
        }

        public TDefinition GetDefinition(Type type)
        {
            if (!TryGetDefinition(type, out var definition))
            {
                throw new ArgumentException($"No definition for type '{type.PrettyPrint()}', have you remembered to load it during EventFlow initialization");
            }

            return definition;
        }

        public IReadOnlyCollection<TDefinition> GetDefinitions(Type type)
        {
            if (!TryGetDefinitions(type, out var definitions))
            {
                throw new ArgumentException($"No definition for type '{type.PrettyPrint()}', have you remembered to load it during EventFlow initialization");
            }

            return definitions;
        }

        public bool TryGetDefinition(Type type, out TDefinition definition)
        {
            if (!TryGetDefinitions(type, out var definitions))
            {
                definition = default(TDefinition);
                return false;
            }

            if (definitions.Count > 1)
            {
                throw new InvalidOperationException($"Type '{type.PrettyPrint()}' has multiple definitions: {string.Join(", ", definitions.Select(d => d.ToString()))}");
            }

            definition = definitions.Single();
            return true;
        }

        public bool TryGetDefinitions(Type type, out IReadOnlyCollection<TDefinition> definitions)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!_definitionsByType.TryGetValue(type, out var list))
            {
                definitions = default(IReadOnlyCollection<TDefinition>);
                return false;
            }

            definitions = list;
            return true;
        }

        protected abstract TDefinition CreateDefinition(int version, Type type, string name);

        private IEnumerable<TDefinition> CreateDefinitions(Type versionedType)
        {
            var hasAttributeDefinition = false;
            foreach (var definitionFromAttribute in CreateDefinitionFromAttribute(versionedType))
            {
                hasAttributeDefinition = true;
                yield return definitionFromAttribute;
            }

            if (hasAttributeDefinition) yield break;
            
            yield return CreateDefinitionFromName(versionedType);
        }

        private TDefinition CreateDefinitionFromName(Type versionedType)
        {
            var match = NameRegex.Match(versionedType.Name);
            if (!match.Success)
            {
                throw new ArgumentException($"Versioned type name '{versionedType.Name}' is not a valid name");
            }

            var version = 1;
            var groups = match.Groups["version"];
            if (groups.Success)
            {
                version = int.Parse(groups.Value);
            }

            var name = match.Groups["name"].Value;
            return CreateDefinition(
                version,
                versionedType,
                name);
        }

        private IEnumerable<TDefinition> CreateDefinitionFromAttribute(Type versionedType)
        {
            return versionedType
                .GetTypeInfo()
                .GetCustomAttributes()
                .OfType<TAttribute>()
                .Select(a => CreateDefinition(a.Version, versionedType, a.Name));
        }
    }
}