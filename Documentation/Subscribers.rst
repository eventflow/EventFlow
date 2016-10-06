Subscribers
===========

Whenever your application needs to act when a specific event is emitted
from your domain you create a class that implement one of the following
two interfaces which is.

-  ``ISubscribeSynchronousTo<TAggregate,TIdentity,TEvent>``: Executed
   synchronous
-  ``ISubscribeAsynchronousTo<TAggregate,TIdentity,TEvent>``: Executed
   asynchronous

Any implemented subscribers needs to be registered to this interface,
either using ``AddSubscriber(...)``, ``AddSubscribers(...)`` or
``AddDefaults(...)`` during initialization. If you have configured a
custom IoC container, you can register the implementations using it
instead.

**NOTE:** The *synchronous* and *asynchronous* here has nothing to do
with the .NET framework keywords ``async``, ``await`` or the Task
Parallel Library. It refers to how the subscribers are executed. Read
below for details.

Synchronous subscribers
-----------------------

Synchronous subscribers in EventFlow are executed one at a time for each
emitted domain event in order. This e.g. guarantees that all subscribers
have been executed when the ``ICommandBus.PublishAsync(...)`` returns.

The ``ISubscribeSynchronousTo<,,>`` is shown here.

.. code-block:: c#

    public interface ISubscribeSynchronousTo<TAggregate, in TIdentity, in TEvent>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TEvent : IAggregateEvent<TAggregate, TIdentity>
    {
      Task HandleAsync(
        IDomainEvent<TAggregate, TIdentity, TEvent> domainEvent,
        CancellationToken cancellationToken);
    }

As synchronous subscribers are by their very nature executed
synchronously, emitting multiple events from an aggregate and letting
subscribers publish new commands based on this can however lead to some
unexpected behavior as "innermost" subscribers will be executed before
first.

1. Aggregate emits events ``Event 1`` and ``Event 2``
2. Subscriber handles ``Event 1`` and publishes a command that results
   in ``Event 3`` being emitted
3. Subscriber handles ``Event 3`` (doesn't affect the domain)
4. Subscriber handles ``Event 2``

In the above example the subscriber will handle the events in the
following order ``Event 1``, ``Event 3`` and then ``Event 2``. While
this *could* occur in a distributed system or executing subscribers on
different threads, its a certainty when using synchronous subscribers.

**IMPORTANT:** By default any exceptions thrown by a subscriber are
**swallowed** by EventFlow after it has been logged as an error.
Depending on the application this might be the preferred behavior, but
in some cases it isn't. If subscriber exception should be thrown, and
thus allowing them to be caught in e.g. command handlers, the behaivor
can be disabled by setting the ``ThrowSubscriberExceptions`` to ``true``
like illustrated here.

.. code-block:: c#

    using (var resolver = EventFlowOptions.New
      .Configure(c => c.ThrowSubscriberExceptions = true)
      .CreateResolver())
    {
      ...
    }

Asynchronous subscribers
------------------------

Asynchronous subscribers in EventFlow are executed using the
``ITaskRunner`` which is basically a thin wrapper around
``Task.Run(...)`` and thus any number of asynchronous subscribers might
still be running when a ``ICommandBus.PublishAsync(...)`` returns.

There are *no* guaranteed order between subscribers or even the order of
which emitted domain events are handled.

The ``ISubscribeAsynchronousTo<,,>`` is shown here and is, besides its
name, identical to its synchronous counterpart.

.. code-block:: c#

    public interface ISubscribeAsynchronousTo<TAggregate, in TIdentity, in TEvent>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TEvent : IAggregateEvent<TAggregate, TIdentity>
    {
      Task HandleAsync(
        IDomainEvent<TAggregate, TIdentity, TEvent> domainEvent,
        CancellationToken cancellationToken);
    }

**NOTE:** Setting ``ThrowSubscriberExceptions = true`` has **no effect**
on asynchronous subscribers.
