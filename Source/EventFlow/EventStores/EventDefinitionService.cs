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
using EventFlow.Aggregates;
using EventFlow.EventSourcing;
using EventFlow.EventSourcing.Events;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public class EventDefinitionService : IEventDefinitionService
    {
        private static readonly Regex EventNameRegex = new Regex(
            @"^(Old){0,1}(?<name>[a-zA-Z]+)(V(?<version>[0-9]+)){0,1}$",
            RegexOptions.Compiled);

        private readonly ILog _log;
        private readonly Dictionary<Type, EventDefinition> _eventDefinitionsByType = new Dictionary<Type, EventDefinition>();
        private readonly Dictionary<string, EventDefinition> _eventDefinitionsByName = new Dictionary<string, EventDefinition>();  

        public EventDefinitionService(
            ILog log)
        {
            _log = log;
        }

        public void LoadEvents(IEnumerable<Type> eventTypes)
        {
            var eventDefinitions = eventTypes.Select(GetEventDefinition).ToList();
            var assemblies = eventDefinitions
                .Select(d => d.Type.Assembly.GetName().Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            _log.Verbose(
                "Added {0} events from these assemblies: {1}",
                eventDefinitions.Count,
                string.Join(", ", assemblies));

            foreach (var eventDefinition in eventDefinitions)
            {
                var key = GetKey(eventDefinition.Name, eventDefinition.Version);
                _eventDefinitionsByName.Add(key, eventDefinition);
            }
        }

        public EventDefinition GetEventDefinition(string eventName, int version)
        {
            var key = GetKey(eventName, version);
            if (!_eventDefinitionsByName.ContainsKey(key))
            {
                throw new ArgumentException($"No event definition for '{eventName}' with version {version}");
            }

            return _eventDefinitionsByName[key];
        }

        public EventDefinition GetEventDefinition(Type eventType)
        {
            if (eventType == null)
            {
                throw new ArgumentNullException(nameof(eventType));
            }
            if (_eventDefinitionsByType.ContainsKey(eventType))
            {
                return _eventDefinitionsByType[eventType];
            }
            if (!typeof(IEvent).IsAssignableFrom(eventType))
            {
                throw new ArgumentException($"Event '{eventType.Name}' is not a DomainEvent");
            }

            var eventDefinition = CreateEventDefinitions(eventType).FirstOrDefault(ed => ed != null);
            if (eventDefinition == null)
            {
                throw new ArgumentException(
                    $"Could not create a event definition for event type '{eventType.Name}'",
                    nameof(eventType));
            }

            _log.Verbose("Added event definition '{0}'", eventDefinition);

            _eventDefinitionsByType.Add(eventType, eventDefinition);

            return eventDefinition;
        }

        private static IEnumerable<EventDefinition> CreateEventDefinitions(Type eventType)
        {
            yield return CreateEventDefinitionFromAttribute(eventType);
            yield return CreateEventDefinitionFromName(eventType);
        }

        private static EventDefinition CreateEventDefinitionFromName(Type eventType)
        {
            var match = EventNameRegex.Match(eventType.Name);
            if (!match.Success)
            {
                throw new ArgumentException($"Event name '{eventType.Name}' is not a valid event name");
            }

            var version = 1;
            var groups = match.Groups["version"];
            if (groups.Success)
            {
                version = int.Parse(groups.Value);
            }

            var name = match.Groups["name"].Value;
            return new EventDefinition(
                version,
                eventType,
                name);
        }

        private static EventDefinition CreateEventDefinitionFromAttribute(Type eventType)
        {
            var eventVersion = eventType.GetCustomAttribute(typeof(EventVersionAttribute), false) as EventVersionAttribute;
            return eventVersion == null
                ? null
                : new EventDefinition(
                    eventVersion.Version,
                    eventType,
                    eventVersion.Name);
        }

        private static string GetKey(string eventName, int version)
        {
            return $"{eventName} - v{version}";
        }
    }
}
