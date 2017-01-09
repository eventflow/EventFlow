// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
            @"^(Old){0,1}(?<name>[a-zA-Z0-9]+?)(V(?<version>[0-9]+)){0,1}$",
            RegexOptions.Compiled);

        private readonly object _syncRoot = new object();
        private readonly ILog _log;
        private readonly ConcurrentDictionary<Type, TDefinition> _definitionsByType = new ConcurrentDictionary<Type, TDefinition>();
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
                .Where(t => !typeof(TTypeCheck)
                .IsAssignableFrom(t))
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
                    .Select(CreateDefinition)
                    .ToList();
                if (!definitions.Any())
                {
                    return;
                }

                _log.Verbose(() =>
                    {
                        var assemblies = definitions
                            .Select(d => d.Type.Assembly.GetName().Name)
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
                    _definitionsByType.TryAdd(definition.Type, definition);

                    Dictionary<int, TDefinition> versions;
                    if (!_definitionByNameAndVersion.TryGetValue(definition.Name, out versions))
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
            Dictionary<int, TDefinition> versions;
            return _definitionByNameAndVersion.TryGetValue(name, out versions)
                ? versions.Values.OrderBy(d => d.Version)
                : Enumerable.Empty<TDefinition>();
        }

        public IEnumerable<TDefinition> GetAllDefinitions()
        {
            return _definitionByNameAndVersion.SelectMany(kv => kv.Value.Values);
        } 

        public bool TryGetDefinition(string name, int version, out TDefinition definition)
        {
            Dictionary<int, TDefinition> versions;
            if (_definitionByNameAndVersion.TryGetValue(name, out versions))
            {
                return versions.TryGetValue(version, out definition);
            }

            definition = null;

            return false;
        }

        public TDefinition GetDefinition(string name, int version)
        {
            TDefinition definition;
            if (!TryGetDefinition(name, version, out definition))
            {
                throw new ArgumentException($"No versioned type definition for '{name}' with version {version} in '{GetType().PrettyPrint()}'");
            }

            return definition;
        }

        public TDefinition GetDefinition(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            TDefinition definition;
            if (!_definitionsByType.TryGetValue(type, out definition))
            {
                throw new ArgumentException($"No definition for type '{type.PrettyPrint()}', have you remembered to load it during EventFlow initialization");
            }

            return definition;
        }

        public bool TryGetDefinition(Type type, out TDefinition definition)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return _definitionsByType.TryGetValue(type, out definition);
        }

        private TDefinition CreateDefinition(Type type)
        {
            var definition = CreateDefinitions(type).FirstOrDefault(d => d != null);
            if (definition == null)
            {
                throw new ArgumentException(
                    $"Could not create a versioned type definition for type '{type.PrettyPrint()}' in '{GetType().PrettyPrint()}'",
                    nameof(type));
            }

            _log.Verbose(() => $"{GetType().PrettyPrint()}: Added versioned type definition '{definition}'");

            return definition;
        }

        protected abstract TDefinition CreateDefinition(int version, Type type, string name);

        private IEnumerable<TDefinition> CreateDefinitions(Type versionedType)
        {
            yield return CreateDefinitionFromAttribute(versionedType);
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

        private TDefinition CreateDefinitionFromAttribute(Type versionedType)
        {
            var attribute = versionedType
                .GetCustomAttributes()
                .OfType<TAttribute>()
                .SingleOrDefault();
            return attribute == null
                ? null
                : CreateDefinition(
                    attribute.Version,
                    versionedType,
                    attribute.Name);
        }
    }
}