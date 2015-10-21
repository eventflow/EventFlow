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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Entities;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Queries;
using EventFlow.Examples.Shipping.Extensions;
using EventFlow.Queries;

namespace EventFlow.Examples.Shipping.Services.Routing
{
    public class RoutingService : IRoutingService
    {
        private readonly IQueryProcessor _queryProcessor;

        public RoutingService(
            IQueryProcessor queryProcessor)
        {
            _queryProcessor = queryProcessor;
        }

        public async Task<Itinerary> CalculateItineraryAsync(Route route, CancellationToken cancellationToken)
        {
            var schedules = await _queryProcessor.ProcessAsync(new GetSchedulesQuery(), cancellationToken).ConfigureAwait(false);
            return CalculateItinerary(route, schedules);
        }

        public Itinerary CalculateItinerary(Route route, IReadOnlyCollection<Schedule> schedules)
        {
            var path = CalculatePath(route, schedules);
            var legs = path.CarrierMovements.Select(m => new TransportLeg(TransportLegId.New, m.DepartureLocationId, m.ArrivalLocationId, m.DepartureTime, m.ArrivalTime, m.Id));
            return new Itinerary(legs);
        }

        private static Path CalculatePath(Route route, IEnumerable<Schedule> schedules)
        {
            var graph = new Graph();
            foreach (var carrierMovement in schedules.SelectMany(s => s.CarrierMovements))
            {
                graph.Add(carrierMovement);
            }

            var paths = new List<Path>
            {
                new Path(0.0, route.DepartureTime, graph.Nodes[route.OriginLocationId.Value])
            };

            while (true)
            {
                if (!paths.Any())
                {
                    throw new Exception("Could not find path");
                }

                var orderedPaths = paths
                    .Where(p => !double.IsPositiveInfinity(p.Distance))
                    .OrderBy(p => p.Distance);
                paths = new List<Path>();

                foreach (var path in orderedPaths)
                {
                    if (path.CurrentNode.Name == route.DestinationLocationId.Value)
                    {
                        return path;
                    }

                    paths.AddRange(path.CurrentNode.Edges.Select(e => CreatePath(path, e.CarrierMovement, e.Target)).Where(p => p != null));
                }
            }
        }

        private static Path CreatePath(Path currentPath, CarrierMovement carrierMovement, Node target)
        {
            if (currentPath.CurrentTime.IsAfter(carrierMovement.DepartureTime))
            {
                return null;
            }

            var distance = (carrierMovement.ArrivalTime - currentPath.CurrentTime).TotalHours;

            return currentPath.AppendAndCreate(
                distance,
                carrierMovement.ArrivalTime,
                target,
                carrierMovement);
        }

        public class Node
        {
            public Node(
                string name)
            {
                Name = name;
            }

            public string Name { get; }
            public List<Edge> Edges { get; } = new List<Edge>();

            public void AddEdge(Node node, CarrierMovement carrierMovement)
            {
                Edges.Add(new Edge(node, carrierMovement));
            }
        }

        public class Edge
        {
            public Edge(
                Node target,
                CarrierMovement carrierMovement)
            {
                Target = target;
                CarrierMovement = carrierMovement;
            }

            public Node Target { get; }
            public CarrierMovement CarrierMovement { get; }
        }

        public class Path
        {
            public Path(double distance, DateTimeOffset currentTime, params Node[] directions)
                : this(distance, currentTime, directions, Enumerable.Empty<CarrierMovement>())
            {
            }

            public Path(double distance, DateTimeOffset currentTime, IEnumerable<Node> directions, IEnumerable<CarrierMovement> carrierMovements)
            {
                Distance = distance;
                CurrentTime = currentTime;
                CarrierMovements = carrierMovements;
                Directions = (directions ?? Enumerable.Empty<Node>()).ToList();
                CurrentNode = Directions.Last();
            }

            public double Distance { get; }
            public Node CurrentNode { get; }
            public IReadOnlyList<Node> Directions { get; }
            public DateTimeOffset CurrentTime { get; }
            public IEnumerable<CarrierMovement> CarrierMovements { get; }

            public Path AppendAndCreate(double distance, DateTimeOffset currentTime, Node node, CarrierMovement carrierMovement)
            {
                return new Path(
                    Distance + distance,
                    currentTime,
                    Directions.Concat(new[] {node}),
                    CarrierMovements.Concat(new [] { carrierMovement}));
            }
        }

        public class Graph
        {
            public Dictionary<string, Node> Nodes = new Dictionary<string, Node>();

            public void Add(CarrierMovement carrierMovement)
            {
                AddNode(carrierMovement.ArrivalLocationId.Value);
                AddNode(carrierMovement.DepartureLocationId.Value);

                Nodes[carrierMovement.DepartureLocationId.Value].AddEdge(Nodes[carrierMovement.ArrivalLocationId.Value], carrierMovement);
            }

            private void AddNode(string name)
            {
                if (!Nodes.ContainsKey(name))
                {
                    Nodes.Add(name, new Node(name));
                }
            }
        }
    }
}