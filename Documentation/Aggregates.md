# Aggregates

Initially before you can create a aggregate, you need to create its
identity. You can create your own implementation by implementing
the `IIdentity` interface or you can use a base class that EventFlow provides
like this.

```csharp
public class TestId : Identity<TestId>
{
  public TestId(string value) : base(value)
  {
  }
}
```

Note that its important to call the constructor argument for `value` as
its significant if you serialize the ID.

Next, to create a new aggregate, simply inherit from `AggregateRoot<,>` like
this, making sure to pass test aggregate own type as the first generic
argument and the identity as the second.

```csharp
public class TestAggregate : AggregateRoot<TestAggregate, TestId>
{
  public TestAggregate(TestId id)
    : base(id)
  {
  }
}
```

## Emitting events

In order to emit an event from an aggregate, call the `protected`
`Emit(...)` method which applies the event and adds it to the list of
uncommitted events.

```csharp
public void Ping()
{
  // Fancy domain logic here...
  Emit(new PingEvent())
}
```

Remember not to do any changes to the aggregate with the these methods, as
as state are only stored through events and how they are applied to the
aggregate root.

## Applying events

Currently EventFlow has three methods of applying events to the aggregate when
emitted or loaded from the event store.

- Create a method called `Apply` that takes the event as argument. To get the
  method signature right, implement the `IEmit<SomeEvent>` on your aggregate
- Register a specific handler for a event using the protected
 `Register<SomeEvent>(e => Handler(e))` from within the constructor
- Override how _all_ events are applied to the aggregate by creating
  an override of the `ApplyEvent(IAggregateEvent<TAggregate, TIdentity> e)`
  method.
