---
title: Aggregates
---

# Aggregates

Before you can create an aggregate, you need to create its identity. You can create your own implementation by implementing the `IIdentity` interface or you can use the base class `Identity<>` that EventFlow provides, like this:

```csharp
[JsonConverter(typeof(SingleValueObjectConverter))]
public class TestId : Identity<TestId>
{
  public TestId(string value) : base(value)
  {
  }
}
```

The `Identity<>` [value object](../additional/value-objects.md) provides generic functionality to create and validate aggregate root IDs. Please read the [documentation](../basics/identity.md) regarding the bundled `Identity<>` type as it provides several useful features, such as different schemes for ID generation, including one that minimizes MSSQL database fragmentation.

The `TestId` class in this example uses a custom JSON converter called `SingleValueObjectConverter`, which is based on the `JsonConverter` class from `Newtonsoft.Json` library. Using this converter is optional but recommended. It makes JSON serialization of value objects [look cleaner and more readable](../additional/value-objects.md#making-pretty-and-clean-json).

Next, to create a new aggregate, simply inherit from `AggregateRoot<,>` like this, making sure to pass the aggregate's own type as the first generic argument and the identity as the second:

```csharp
public class TestAggregate : AggregateRoot<TestAggregate, TestId>
{
  public TestAggregate(TestId id)
    : base(id)
  {
  }
}
```

## Events

In an event-sourced system like EventFlow, aggregate root data is stored on events.

```csharp
public class PingEvent : AggregateEvent<TestAggregate, TestId>
{
  public string Data { get; }

  public PingEvent(string data)
  {
      Data = data;
  }
}
```

Please make sure to read the section on [value objects and events](../additional/value-objects.md) for some important notes on creating events.

## Emitting events

In order to emit an event from an aggregate, call the `protected` `Emit(...)` method, which applies the event and adds it to the list of uncommitted events.

```csharp
public class TestAggregate : AggregateRoot<TestAggregate, TestId>
{
  // Other details are omitted for clarity

  public void Ping(string data)
  {
    // Fancy domain logic here that validates aggregate state...

    if (string.IsNullOrEmpty(data))
    {
      throw DomainError.With("Ping data is empty");
    }

    Emit(new PingEvent(data));
  }
}
```

Remember not to make any changes to the aggregate with these methods, as the state is only stored through events.

## Applying events

Currently, EventFlow has four methods of applying events to the aggregate when emitted or loaded from the event store. Which you choose is up to you. Implementing `IEmit<SomeEvent>` is the most convenient, but will expose public `Apply` methods.

- Create a method called `Apply` that takes the event as an argument. To get the method signature right, implement the `IEmit<SomeEvent>` on your aggregate. This is the default fallback, and you will get an exception if no other strategies are configured. Although you *can* implement `IEmit<SomeEvent>`, it's optional. The `Apply` methods can be `protected` or `private`.

  ```csharp
  public class TestAggregate :
    AggregateRoot<TestAggregate, TestId>,
    IEmit<PingEvent>
  {
    // Other details are omitted for clarity

    public void Apply(PingEvent aggregateEvent)
    {
      // Change the aggregate here
    }
  }
  ```

- Create a state object by inheriting from `AggregateState<,,>` and registering it using the protected `Register(...)` in the aggregate root constructor.
- Register a specific handler for an event using the protected `Register<SomeEvent>(e => Handler(e))` from within the constructor.
- Register an event applier using `Register(IEventApplier eventApplier)`, which could be, for example, a state object.

## Modifying the Aggregate

EventFlow provides several ways to change the state of an aggregate.

### Using `IAggregateStore` interface

The `IAggregateStore.UpdateAsync` method allows to load, modify and save the aggregate in a single method call. Here's an example of a controller that modifies `TestAggregate`:

```csharp 
public class TestController(IAggregateStore aggregateStore) : ControllerBase
{
  public async Task Ping(
    Guid id,
    CancellationToken cancellationToken)
  {    
    var testId = TestId.With(id);
    var sourceId = TestId.New;

    await aggregateStore.UpdateAsync<TestAggregate, TestId>(
        testId,
        sourceId,
        (aggregate, cancellationToken) =>
        {
          aggregate.Ping("ping");
          return Task.CompletedTask;
        },
        cancellationToken);
  }
}
```

In this example `sourceId` is a unique random identifier that prevents the same operation from being applied twice. To use an aggregate identity as the source ID, it must implement the `ISourceId` interface:

```csharp
public class TestId : Identity<TestId>, ISourceId 
{
  // Other details are omitted for clarity
}
```

It is also possible to load, modify and save the aggregate manually using `LoadAsync` and `StoreAsync` methods.

```csharp
// Load the aggregate from the store
var testId = TestId.With(id);
var aggregate = await aggregateStore.LoadAsync<TestAggregate, TestId>(
  testId,
  CancellationToken.None);

// Call the method to change the aggregate state
aggregate.Ping("ping");

// Save the changes
var sourceId = TestId.New;
await aggregateStore.StoreAsync<TestAggregate, TestId>(
  aggregate,
  sourceId,
  CancellationToken.None);
```

### Using the CQRS approach

Another way to change the aggregate is by following the CQRS (Command Query Responsibility Segregation) pattern.

```csharp
public class TestController(ICommandBus commandBus) : ControllerBase
{
  public async Task Ping(
    Guid id,
    CancellationToken cancellationToken)
  {
    var testId = TestId.With(id);

    // Create a command with the required data
    var command = new PingCommand(testId)
    {
      Data = "ping",
    };

    // Publish the command using the command bus
    await commandBus.PublishAsync(command, cancellationToken);
  }
}
```

For more details on commands and command handlers, check the [documentation](../basics/commands.md).

## Reading aggregate events

To read events for a specific aggregate, you can use the `IEventStore` interface. This allows you to load events for an aggregate by its identity.

### Load all events

In the example below, the `GetAuditLog` method loads **all events** for the specified aggregate and maps them to a DTO.

```csharp
public class TestController(IEventStore eventStore) : ControllerBase
{
  public async Task<IEnumerable<EventDto>> GetAuditLog(
    Guid id,
    CancellationToken cancellationToken)
  {
    var testId = TestId.With(id);

    var events = await eventStore.LoadEventsAsync<TestAggregate, TestId>(
        testId,
        cancellationToken);

    return events.Select(e => new EventDto
    {
      EventType = e.EventType.Name,
      Timestamp = e.Timestamp
    });
  }
}

public class EventDto
{
  public required string EventType { get; init; }
  public required DateTimeOffset Timestamp { get; init; }
}
```

### Load events for a specific range

You can also load events for a specific range. In the example below, `from` and `to` represent the range of sequence numbers for the events you want to load. This can be useful for pagination or retrieving a specific subset of events for an aggregate.

```csharp
public class TestController(IEventStore eventStore) : ControllerBase
{
  public async Task<IEnumerable<EventDto>> GetEventsInRange(
    Guid id,
    int from,
    int to,
    CancellationToken cancellationToken)
  {
    var testId = TestId.With(id);

    var events = await eventStore.LoadEventsAsync<TestAggregate, TestId>(
        testId,
        from,
        to,
        cancellationToken);

    return events.Select(e => new EventDto
    {
      EventType = e.EventType.Name,
      Timestamp = e.Timestamp
    });
  }
}
```
