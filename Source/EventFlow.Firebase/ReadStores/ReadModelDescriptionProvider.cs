using System;
using EventFlow.Firebase.ValueObjects;
using EventFlow.ReadStores;
using System.Collections.Concurrent;
using EventFlow.Extensions;

namespace EventFlow.Firebase.ReadStores
{
    public class ReadModelDescriptionProvider : IReadModelDescriptionProvider
    {
        private static readonly ConcurrentDictionary<Type, ReadModelDescription> NodeNames 
            = new ConcurrentDictionary<Type, ReadModelDescription>();

        public ReadModelDescription GetReadModelDescription<TReadModel>() 
            where TReadModel : IReadModel
        {
            // TODO: Add an Attribute to override the default root node

            return NodeNames.GetOrAdd(typeof(TReadModel), t => 
            {
                var nodeName = $"eventflow-{typeof(TReadModel).PrettyPrint().ToLowerInvariant()}";
                return new ReadModelDescription(new RootNodeName(nodeName));
            });
        }
    }
}
