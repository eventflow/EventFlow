# Event upgrade
At some point you might find the need to replace a event with zero or more
events. Some use cases might be

* A previous application version introduced a domain error in the form of a
  wrong event being emitted from the aggregate
* Domain has changed, either from a change in requirements or simply from a
  better understanding of the domain

EventFlow event upgraders are invoked whenever the event stream is loaded from
the event store. Each event upgrader receives the entire event stream one event
at a time.

Note that the _ordering_ of event upgraders is important as you might implement
to upgraders, one upgrade a event from version one to version two and then another
upgrading vesion two to version three. EventFlow orders the event upgraders by
name before starting the event upgrade.

## Example - removing a damaged event

To remove an event, simply check and only return the event if its no the event
you want to remove.

```csharp
public class DamagedEventRemover : IEventUpgrader<MyAggregate, MyId>
{
  public IEnumerable<IDomainEvent<TestAggregate, TestId>> Upgrade(
    IDomainEvent<TestAggregate, TestId> domainEvent)
  {
    var damagedEvent = domainEvent as IDomainEvent<MyAggregate, MyId, DamagedEvent>;
    if (damagedEvent == null)
    {
      yield return domainEvent;
    }
  }
}
```

## Example - replace event

To one event to another, you should use the `IDomainEventFactory.Upgrade` to
help migrate meta data and create the new event.

```csharp
public class UpgradeMyEventV1ToMyEventV2 : IEventUpgrader<MyAggregate, MyId>
{
  private readonly IDomainEventFactory _domainEventFactory;

  public UpgradeTestEventV1ToTestEventV2(IDomainEventFactory domainEventFactory)
  {
    _domainEventFactory = domainEventFactory;
  }

  public IEnumerable<IDomainEvent<TestAggregate, TestId>> Upgrade(
    IDomainEvent<TestAggregate, TestId> domainEvent)
  {
    var myEventV1 = domainEvent as IDomainEvent<MyAggregate, MyId, MyEventV1>;
    yield return myEventV1 == null
      ? domainEvent
      : _domainEventFactory.Upgrade<MyAggregate, MyId>(
        domainEvent, new MyEventV2());
  }
}
```
