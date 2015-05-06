# Aggregates

Initially before you can create a aggregate, you need to create its
identity. You can create your own implementation by implementing
the `IIdentity` interface or you can use one EventFlow provides like
this.

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

Next, to create a new aggregate, simply inherit from `AggregateRoot<>` like
this, making sure to pass test aggregate own type as the generic
argument.

```csharp
public class TestAggregate : AggregateRoot<TestAggregate>
{
  public TestAggregate(TestId id)
    : base(id)
  {
  }
}
```

## Events

In order to emit an event from an aggregate, call the `protected`
`Emit(...)` method and apply the corresponding `Apply(...)` method
for that event. You should include the `IEmit<>` in the list of
interfaces for your aggregate as it helps to ensure that the method
signature is correct.

```csharp
public void Ping()
{
  // Fancy domain logic here...
  Emit(new PingEvent())
}

public void Apply(PingEvent e)
{
  // Save ping state here  
}
```
