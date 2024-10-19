---
title: Aggregates
---

# Aggregates

Before you can create an aggregate, you need to create its identity. You can create your own implementation by implementing the `IIdentity` interface or you can use the base class `Identity<>` that EventFlow provides, like this:

```csharp
public class TestId : Identity<TestId>
{
  public TestId(string value) : base(value)
  {
  }
}
```

The `Identity<>` [value object](../additional/value-objects.md) provides generic functionality to create and validate aggregate root IDs. Please read the documentation regarding the bundled `Identity<>` type as it provides several useful features, such as different schemes for ID generation, including one that minimizes MSSQL database fragmentation.

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
public void Ping(string data)
{
  // Fancy domain logic here that validates aggregate state...

  if (string.IsNullOrEmpty(data))
  {
    throw DomainError.With("Ping data is empty");
  }

  Emit(new PingEvent(data));
}
```

Remember not to make any changes to the aggregate with these methods, as the state is only stored through events.

## Applying events

Currently, EventFlow has four methods of applying events to the aggregate when emitted or loaded from the event store. Which you choose is up to you. Implementing `IEmit<SomeEvent>` is the most convenient, but will expose public `Apply` methods.

- Create a method called `Apply` that takes the event as an argument. To get the method signature right, implement the `IEmit<SomeEvent>` on your aggregate. This is the default fallback, and you will get an exception if no other strategies are configured. Although you *can* implement `IEmit<SomeEvent>`, it's optional. The `Apply` methods can be `protected` or `private`.
- Create a state object by inheriting from `AggregateState<,,>` and registering it using the protected `Register(...)` in the aggregate root constructor.
- Register a specific handler for an event using the protected `Register<SomeEvent>(e => Handler(e))` from within the constructor.
- Register an event applier using `Register(IEventApplier eventApplier)`, which could be, for example, a state object.
