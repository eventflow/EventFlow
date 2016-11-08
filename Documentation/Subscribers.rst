.. _subscribers:

Subscribers
============

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

.. NOTE::

    The *synchronous* and *asynchronous* here has nothing to do
    with the .NET framework keywords ``async``, ``await`` or the Task
    Parallel Library. It refers to how the subscribers are executed. Read
    below for details.


.. _subscribers-sync:

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

.. _out-of-order-event-subscribers:

Out of order events
^^^^^^^^^^^^^^^^^^^

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


Exceptions swallowed by default
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

By default any exceptions thrown by a subscriber are **swallowed**
by EventFlow after it has been logged as an error. Depending on the
application this might be the preferred behavior, but in some cases
it isn't. If subscriber exception should be thrown, and thus allowing
them to be caught in e.g. command handlers, the behaivor can be disabled
by setting the ``ThrowSubscriberExceptions`` to ``true`` like illustrated
here.

.. code-block:: c#

    using (var resolver = EventFlowOptions.New
      .Configure(c => c.ThrowSubscriberExceptions = true)
      .CreateResolver())
    {
      ...
    }


.. _subscribers-async:

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

.. NOTE::

    Setting ``ThrowSubscriberExceptions = true`` has **no effect**
    on asynchronous subscribers.


Subscribe to every event
------------------------

Instead of subscribing to every single domain, you can register an
implementation of ``ISubscribeSynchronousToAll`` which is defined as
shown here.

.. code-block:: c#

    public interface ISubscribeSynchronousToAll
    {
        Task HandleAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken);
    }

Any registered implementations will be notified for every domain event
emitted.


.. _subscribers-rabbitmq:

RabbitMQ
^^^^^^^^

See :ref:`RabbitMQ setup <setup-rabbitmq>` for details on how to get
started using RabbitMQ_.

After RabbitMQ has been configured, all domain events are published
to a exchange named ``eventflow`` with routing keys in the following
format.

::

    eventflow.domainevent.[Aggregate name].[Event name].[Event version]

Which will be the following for an event named ``CreateUser`` version
``1`` for the ``MyUserAggregate``.

::

    eventflow.domainevent.my-user.create-user.1

Note the lowercasing and adding of ``-`` whenever there's a capital
letter.

All the above is the default behavior, if you don't like it replace e.g.
the service ``IRabbitMqMessageFactory`` to customize what routing key or
exchange to use. Have a look at how
`EventFlow <https://github.com/rasmus/EventFlow>`__ has done its
implementation to get started.

.. _RabbitMQ: https://www.rabbitmq.com/
