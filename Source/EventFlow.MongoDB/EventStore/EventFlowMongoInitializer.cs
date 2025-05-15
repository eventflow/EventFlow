using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventFlow.MongoDB.EventStore
{
    public class EventFlowMongoInitializer
    {
        private readonly IServiceProvider _serviceProvider;

        public EventFlowMongoInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize()
        {
            using var scope = _serviceProvider.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<IMongoDbEventPersistenceInitializer>();
            initializer.Initialize();
        }
    }
}
