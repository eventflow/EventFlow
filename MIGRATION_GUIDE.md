# WORK IN PROGRESS

This is still just a collection of notes. Will slowly evolve as the API for
1.0 becomes clear.

# Migration guide 0.x to 1.x

## Data in event stores

Upgrading EventFlow should **never** break existing data in event stores, not even
between major versions. All data currently in event stores will work with 1.x
releases. However, it might not be possible to do a rollback from 1.x to 0.x.

## NuGet packages removed

- `EventFlow.Autofac` use native Autofac integration packages for Microsoft
  dependency injection
- `EventFlow.DependencyInjection` now integrated into the core package
  `Microsoft.Extensions.DependencyInjection`
- `EventFlow.Owin` switch to ASP.NET Core

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


