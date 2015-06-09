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

A new instance of a event upgrader is created each time an aggregate is loaded.
This enables you to store information from previous events on the upgrader
instance to be used later, e.g. to determine an action to take on a event
or provide additional information for a new event.

Note that the _ordering_ of event upgraders is important as you might implement
two upgraders, one upgrade a event from V1 to V2 and then another upgrading V2
to V3. EventFlow orders the event upgraders by name before starting the event
upgrade.

**Be careful** if working with event upgraders that return zero or more than one
event, as this have an influence on the aggregate version and you need to make
sure that the aggregate sequence number on upgraded events have a valid value.

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
