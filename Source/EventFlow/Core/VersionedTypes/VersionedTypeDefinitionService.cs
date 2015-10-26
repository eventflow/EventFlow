// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Core.VersionedTypes
{
    public abstract class VersionedTypeDefinitionService<TAttribute, TDefinition>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Regex NameRegex = new Regex(
            @"^(Old){0,1}(?<name>[a-zA-Z]+)(V(?<version>[0-9]+)){0,1}$",
            RegexOptions.Compiled);

        private readonly ILog _log;
        private readonly Dictionary<Type, TDefinition> _definitionsByType = new Dictionary<Type, TDefinition>();
        private readonly Dictionary<string, TDefinition> _definitionsByName = new Dictionary<string, TDefinition>();

        protected VersionedTypeDefinitionService(
            ILog log)
        {
            _log = log;
        }

        protected void Load(IEnumerable<Type> types)
        {
            if (types == null)
            {
                return;
            }

            var definitions = types.Select(GetDefinition).ToList();
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
                var key = GetKey(definition.Name, definition.Version);
                if (!_definitionsByName.ContainsKey(key))
                {
                    _definitionsByName.Add(key, definition);
                }
                else
                {
                    _log.Information(
                        "Already loaded versioned type '{0}' v{1}, skipping it",
                        definition.Name,
                        definition.Version);
                }
            }
        }

        protected bool TryGetDefinition(string name, int version, out TDefinition definition)
        {
            var key = GetKey(name, version);
            return _definitionsByName.TryGetValue(key, out definition);
        }

        protected TDefinition GetDefinition(string name, int version)
        {
            TDefinition definition;
            if (!TryGetDefinition(name, version, out definition))
            {
                throw new ArgumentException($"No versioned type definition for '{name}' with version {version} in '{GetType().PrettyPrint()}'");
            }

            return definition;
        }

        protected TDefinition GetDefinition(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (_definitionsByType.ContainsKey(type))
            {
                return _definitionsByType[type];
            }

            var definition = CreateDefinitions(type).FirstOrDefault(d => d != null);
            if (definition == null)
            {
                throw new ArgumentException(
                    $"Could not create a versioned type definition for type '{type.PrettyPrint()}' in '{GetType().PrettyPrint()}'",
                    nameof(type));
            }

            _log.Verbose(() => $"{GetType().PrettyPrint()}: Added versioned type definition '{definition}'");

            _definitionsByType.Add(type, definition);

            return definition;
        }

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

        protected abstract TDefinition CreateDefinition(int version, Type type, string name);

        private static string GetKey(string versionedTypeName, int version)
        {
            return $"{versionedTypeName} - v{version}";
        }
    }
}
