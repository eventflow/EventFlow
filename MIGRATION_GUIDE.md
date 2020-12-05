# Migration guide 0.x to 1.x

## Initializing EventFlow

There are a few ways you can initialize EventFlow.

```csharp
var eventFlowOptions = EventFlowOptions.New()
```

```csharp
var eventFlowOptions = EventFlowOptions.New(serviceCollection)
```

```csharp
serviceCollection.AddEventFlow(o => 
    // ...
    )
```

## Aligning with Microsoft extension packages

Several types have been removed from EventFlow in order to align
with the Microsoft extension packages.

- `ILog` use `ILogger` from `Microsoft.Extensions.Logger.Abstractions`
- `IResolver` use `IServiceProvider`
  from `Microsoft.Extensions.DependencyInjection.Abstractions`


## Removal of non-async method

Several non-async methods have been removed as well as the
`EventFlow.Core.AsyncHelper` which was used to implement these methods
without introducing deadlocks when running in some .NET Framework
environments.

- `IAggregateStore.Load`
- `IAggregateStore.Store`
- `IAggregateStore.Update`
- `ICommandBus.Publish`
- `IEventStore.LoadAggregate`
- `IEventStore.LoadEvents`
- `IEventStore.LoadAllEvents`
- `IQueryProcessor.Process`
- `IReadModelPopulator.Populate`
- `IReadModelPopulator.Purge`


