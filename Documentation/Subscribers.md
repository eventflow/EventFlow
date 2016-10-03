# Subscribers
Whenever your application needs to act when a specific event is emitted from
your domain you create a class that implement the
`ISubscribeSynchronousTo<TAggregate,TIdentity,TEvent>` interface which is
defined as showed here.

```csharp
public interface ISubscribeSynchronousTo<TAggregate, in TIdentity, in TEvent>
    where TAggregate : IAggregateRoot<TIdentity>
    where TIdentity : IIdentity
    where TEvent : IAggregateEvent<TAggregate, TIdentity>
{
  Task HandleAsync(
    IDomainEvent<TAggregate, TIdentity, TEvent> domainEvent,
    CancellationToken cancellationToken);
}
```

Any implemented subscribers needs to be registered to this interface, either
using `AddSubscriber(...)`, `AddSubscribers(...)` or `AddDefaults(...)` during
initialization. If you have configured a custom IoC container, you can register
the implementations using it instead.

**IMPORTANT:** By default any exceptions thrown by a subscriber are
__shallowed__ by EventFlow after it has been logged as an error. Depending on
the application this might be the preferred behavior, but in some cases it isn't.
If subscriber exception are to be throw, and thus allowing them to be caught in
e.g. command handlers, the behaivor can be disabled by setting the
`ThrowSubscriberExceptions` to `true` like illustrated here.

```csharp
using (var resolver = EventFlowOptions.New
  .Configure(c => c.ThrowSubscriberExceptions = true)
  .CreateResolver())
{
  ...
}
```
