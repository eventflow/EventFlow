# EventFlow
EventFlow is a basic CQRS+ES framework designed to be easy to use
and

### Features

* CQRS+ES framework
* Async/await first
* Highly configurable and extendable
* Easy to use
* No use of threads or background workers making it "web friendly"

### Concepts

Use the links to navigate to the documentation.

* [**Aggregates:**](./Documentation/Aggregates.md) Domains object
  that guarantees the consistency of changes being made within
  each aggregate
* **Command bus:** Entry point for all commands
* **Event store:** Storage of the event stream for aggregates
* **Read models:** Denormalized representation of aggregate events
  optimized for reading fast.
* [**Metadata providers:**](./Documentation/MetadataProviders.md)
  Factories that provide additional information for each aggregate
  event, e.g. the IP of the user behind the event being emitted.
  EventFlow has several that can be used.

## Full example
Here's an example on how to use the in-memory event store (default)
and a in-memory read model store.

```csharp
using (var resolver = EventFlowOptions.New
    .AddEvents(typeof (TestAggregate).Assembly)
    .UseInMemoryReadStoreFor<TestAggregate, TestReadModel>()
    .CreateResolver())
{
  var commandBus = resolver.Resolve<ICommandBus>();
  var eventStore = resolver.Resolve<IEventStore>();
  var readModelStore = resolver.Resolve<IInMemoryReadModelStore<
    TestAggregate,
    TestReadModel>>();
  var id = Guid.NewGuid().ToString();

  // Publish a command
  await commandBus.PublishAsync(new TestACommand(id));

  // Load aggregate
  var testAggregate = await eventStore.LoadAggregateAsync<TestAggregate>(id);

  // Get read model from in-memory read store
  var testReadModel = await readModelStore.GetAsync(id);
}
```

## MSSQL event store provider
To use the MSSQL event store provider you need to install the NuGet
package `EventFlow.ReadStores.MsSql` and configure the connection
string as shown here.

```csharp
var resolver = EventFlowOptions.New
  .AddEvents(EventFlowTest.Assembly)
  .ConfigureMsSql(MsSqlConfiguration.New
    .SetConnectionString(@"Server=SQLEXPRESS;User Id:sa;Password=?"))
  .UseEventStore<MsSqlEventStore>()
  ...
  .CreateResolver();
```

## Useful links

* [https://msdn.microsoft.com/en-us/library/jj554200.aspx](CQRS Journey by Microsoft)
