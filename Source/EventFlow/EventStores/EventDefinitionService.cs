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
using System.Text.RegularExpressions;
using EventFlow.Aggregates;

namespace EventFlow.EventStores
{
    public class EventDefinitionService : IEventDefinitionService
    {
        private readonly Dictionary<Type, EventDefinition> _eventDefinitionsByType = new Dictionary<Type, EventDefinition>();
        private readonly Dictionary<string, EventDefinition> _eventDefinitionsByName = new Dictionary<string, EventDefinition>();  
        private readonly Regex _eventNameRegex = new Regex(@"^(Old){0,1}(?<name>[a-zA-Z]+)(V(?<version>[0-9]+)){0,1}$", RegexOptions.Compiled);

        public void LoadEvents(IEnumerable<Type> eventTypes)
        {
            foreach (var eventDefinition in eventTypes.Select(GetEventDefinition))
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
                throw new ArgumentException(string.Format(
                    "No event definition for '{0}' with version {1}",
                    eventName,
                    version));
            }

            return _eventDefinitionsByName[key];
        }

        public EventDefinition GetEventDefinition(Type eventType)
        {
            if (_eventDefinitionsByType.ContainsKey(eventType))
            {
                return _eventDefinitionsByType[eventType];
            }

            if (!typeof(IAggregateEvent).IsAssignableFrom(eventType))
            {
                throw new ArgumentException(string.Format(
                    "Event '{0}' is not a DomainEvent", eventType.Name));
            }

            var match = _eventNameRegex.Match(eventType.Name);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format(
                    "Event name '{0}' is not a valid event name",
                    eventType.Name));
            }

            var version = 1;
            var groups = match.Groups["version"];
            if (groups.Success)
            {
                version = int.Parse(groups.Value);
            }

            var name = match.Groups["name"].Value;
            var eventDefinition = new EventDefinition(
                version,
                eventType,
                name);

            _eventDefinitionsByType.Add(eventType, eventDefinition);

            return eventDefinition;
        }

        private static string GetKey(string eventName, int version)
        {
            return string.Format("{0} - v{1}", eventName, version);
        }
    }
}
