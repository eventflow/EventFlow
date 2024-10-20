---
layout: default
title: Subscribers
parent: Basics
nav_order: 2
---

# Subscribers

Whenever your application needs to perform an action when a specific 
event is emitted from your domain, you create a class that implements
one of the following two interfaces:

-  `ISubscribeSynchronousTo<TAggregate,TIdentity,TEvent>`: Executed
   synchronously
-  `ISubscribeAsynchronousTo<TAggregate,TIdentity,TEvent>`: Executed
   asynchronously

Any subscribers that you implement need to be registered to this interface
using either `AddSubscriber(...)`, `AddSubscribers(...)` or
`AddDefaults(...)` during initialization. If you have configured a
custom IoC container, you can register the implementations using it
instead.

!!! warning
    The *synchronous* and *asynchronous* here has nothing to do
    with the .NET framework keywords `async`, `await` or the Task
    Parallel Library. It refers to how the subscribers are executed. Read
    below for details.


## Synchronous subscribers

Synchronous subscribers in EventFlow are executed one at a time for each
emitted domain event in order. This e.g. guarantees that all subscribers
have been executed when the `ICommandBus.PublishAsync(...)` returns.

The `ISubscribeSynchronousTo<,,>` interface is shown here.

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

### Out of order events

As synchronous subscribers are by their very nature executed
synchronously, emitting multiple events from an aggregate and letting
subscribers publish new commands based on this can lead to some
unexpected behavior as "innermost" subscribers will be executed before
the next "outer" event is handled by the subscriber.

1. Aggregate emits events `Event 1` and `Event 2`
2. Subscriber handles `Event 1` and publishes a command that results
   in `Event 3` being emitted
3. Subscriber handles `Event 3` (doesn't affect the domain)
4. Subscriber handles `Event 2`

In the above example the subscriber will handle the events in the
following order `Event 1`, `Event 3` and then `Event 2`. While
this *could* occur in a distributed system or when executing subscribers on
different threads, it is a certainty when using synchronous subscribers.


### Exceptions swallowed by default

By default any exceptions thrown by a subscriber are **swallowed**
by EventFlow after it has been logged as an error. Depending on the
application this might be the preferred behavior, but in some cases
it isn't. If a subscriber exception should be thrown, and thus allowing
them to be caught in e.g. command handlers, the behaivor can be disabled
by setting the `ThrowSubscriberExceptions` to `true` when configuring EventFlow.

## Asynchronous subscribers

Asynchronous subscribers in EventFlow are executed using a scheduled job.

!!! warning
    Asynchronous subscribers are **disabled by default** and must be
    enabled using the `IsAsynchronousSubscribersEnabled` configuration.

!!! attention
    Since asynchronous subscribers are executed using a job, its important
    to configure proper job scheduling. The `EventFlow.Hangfire` NuGet 
    package integrates with the 'HangFire Job Scheduler <https://www.hangfire.io>, 
    and provides a usable solution to this requirement.

The `ISubscribeAsynchronousTo<,,>` is shown here and is, besides its
name, identical to its synchronous counterpart.


```csharp
public interface ISubscribeAsynchronousTo<TAggregate, in TIdentity, in TEvent>
  where TAggregate : IAggregateRoot<TIdentity>
  where TIdentity : IIdentity
  where TEvent : IAggregateEvent<TAggregate, TIdentity>
{
  Task HandleAsync(
    IDomainEvent<TAggregate, TIdentity, TEvent> domainEvent,
    CancellationToken cancellationToken);
}
```

!!! danger
    Setting `ThrowSubscriberExceptions = true` has **no effect**
    on asynchronous subscribers.


## Subscribe to every event

Instead of subscribing to every single domain, you can register an
implementation of `ISubscribeSynchronousToAll` which is defined as
shown here.

```csharp
public interface ISubscribeSynchronousToAll
{
  Task HandleAsync(
    IReadOnlyCollection<IDomainEvent> domainEvents,
    CancellationToken cancellationToken);
}
```

Any registered implementations will be notified for every domain event
emitted.


### RabbitMQ

See [RabbitMQ setup](../integration/rabbitmq.md) for details on how to get
started using RabbitMQ_.

After RabbitMQ has been configured, all domain events are published
to an exchange named `eventflow` with routing keys in the following
format.

```
eventflow.domainevent.[Aggregate name].[Event name].[Event version]
```

Which will be the following for an event named `CreateUser` version
`1` for the `MyUserAggregate`.

```
eventflow.domainevent.my-user.create-user.1
```

Note the lowercasing and adding of `-` whenever there's a capital
letter.

All the above is the default behavior. If you don't like it, replace the 
registered message factory service `IRabbitMqMessageFactory` to 
customize what routing key or exchange to use. Have a look at how
`EventFlow <https://github.com/rasmus/EventFlow>`__ has done its
implementation to get started.
