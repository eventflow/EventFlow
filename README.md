# EventFlow
EventFlow is a basic CQRS+ES framework.

### Features

* Async/await all the way through
* Highly configurable and extendable
* Easy to use

## Full example
Here's an example on how to use the in-memory event store (default)
and the in-memory read model store.

```csharp
using (var resolver = EventFlowOptions.New
    .AddEvents(typeof (TestAggregate).Assembly)
    .AddMetadataProvider<AddGuidMetadataProvider>()
    .UseInMemoryReadStoreFor<TestAggregate, TestReadModel>()
    .CreateResolver())
{
  var commandBus = resolver.Resolve<ICommandBus>();
  var eventStore = resolver.Resolve<IEventStore>();
  var readModelStore = resolver.Resolve<IInMemoryReadModelStore<
    TestAggregate,
    TestReadModel>>();
  var id = Guid.NewGuid().ToString();

  // Publish command (PublishAsync exists)
  commandBus.Publish(new TestACommand(id));

  // Load aggregate (LoadAggregateAsync exists)
  var testAggregate = eventStore.LoadAggregate<TestAggregate>(id);

  // Get read model from in-memory read store
  var testReadModel = readModelStore.Get(id);
}
```
