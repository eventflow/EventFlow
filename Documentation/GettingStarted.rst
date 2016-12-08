.. _getting-started:

Getting started
===============

Initializing EventFlow always start with an ``EventFlowOptions.New`` as this
performs the initial bootstrap and starts the fluent configuration API. The
very minimum initialization of EventFlow can be done in a single line, but
wouldn't serve any purpose as no domain has been configured.

.. code-block:: c#

  var resolver = EventFlowOptions.New.CreateResolver();


The above line does configures several important defaults

- In-memory :ref:`event store <eventstores>`
- Console logger
- A "null" snapshot store, that merely writes a warning if used (no need to
  do anything before going to production if you aren't planning to use
  snapshots)
- And lastly, default implementations of all the internal parts of EventFlow

.. IMPORTANT::
    Before using EventFlow in a production environment, you should configure an
    alternative **event store** and another **logger** that sends log messages
    to your production log store.


To start using EventFlow, a domain must be configure which consists of the
following parts

- :ref:`Aggregate <aggregates>`
- :ref:`Aggregate identity <identity>`
- :ref:`Aggregate events <events>`
- :ref:`Commands and command handlers <commands>` (optional, but highly recommended)

In addition to the above, EventFlow provides several optional features. Whether
or not these features are utilized, depends on the application in which
EventFlow is used.

- :ref:`Read models <read-stores>`
- :ref:`Subscribers <subscribers>`
- :ref:`Event upgraders <event-upgrade>`
- :ref:`Queries <queries>`
- :ref:`Jobs <jobs>`
- :ref:`Snapshots <snapshots>`
- :ref:`Sagas <sagas>`
- :ref:`Metadata providers <metadata-providers>`

Example application
-------------------

To get started, we start with our entire example application which consists of
one of each of the required parts: aggregate, event, aggregate identity, command
and a command handler. After we will go through the individual parts created.

.. NOTE::
    The example code provided here is located within the EventFlow code base
    exactly as shown, so if you would like to debug and step through the
    entire flow, checkout the code and execute the ``GettingStartedExample``
    test.


All classes create for the example application are prefixed with ``Example``.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleTests.cs
  :linenos:
  :dedent: 12
  :language: c#
  :lines: 41-76

The above example publishes the ``ExampleCommand`` to the aggregate with the
``exampleId`` identity with the magical value of ``42``. After the command has
been published, the accompanying read model ``ExampleReadModel`` is fetched
and we verify that the magical number has reached it.

During the execution of the example application, a single event is emitted and
stored in the in-memory event store. The JSON for the event is shown here.

.. code-block:: json

    {
      "MagicNumber": 42
    }

The event data itself is strait forward as its merely the JSON serialization of
an instance of the type ``ExampleEvent`` with the value we defined. A bit more
interesting is the meta data that EventFlow stores along the event, which is
used by the EventFlow event store.

.. code-block:: json

    {
      "timestamp": "2016-11-09T20:56:28.5019198+01:00",
      "aggregate_sequence_number": "1",
      "aggregate_name": "ExampleAggrenate",
      "aggregate_id": "example-c1d4a2b1-c75b-4c53-ae44-e67ee1ddfd79",
      "event_id": "event-d5622eaa-d1d3-5f57-8023-4b97fabace90",
      "timestamp_epoch": "1478721389",
      "batch_id": "52e9d7e9-3a98-44c5-926a-fc416e20556c",
      "source_id": "command-69176516-07b7-4142-beaf-dba82586152c",
      "event_name": "example",
      "event_version": "1"
    }

All the built-in meta data is available on each instance of ``IDoamainEvent<,,>``,
which is accessible from event handlers for e.g. read models or subscribers. It
also possible create your own :ref:`meta data providers <metadata-providers-custom>`
or add additional EventFlow built-in providers as needed.


Aggregate identity
------------------

The aggregate ID is in EventFlow represented as a value objected that inherits
from the ``IIdentity`` interface. You can provide your own implementation, but
EventFlow provides a convenient implementation that will suit most needs.  Be
sure to read the read the section about the :ref:`Identity\<\> <identity>` class
to get details on how to use it.

For our example application we use the built-in class making the implementation
very simple.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleId.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 29-34


Aggregate
---------

Now we'll take a look at the ``ExampleAggrenate``. Its rather simple as the
only thing it can, is apply the magic number once.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleAggrenate.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 30-55

Be sure to read the section on :ref:`aggregates <aggregates>` to get all the
details right, but for now the most important thing to note, is that the state
of the aggregate (updating the ``_magicNumber`` variable) happens in the
``Apply(ExampleEvent)`` method. This is the event sourcing part of EventFlow in
effect. As state changes are only saved as events, mutating the aggregate state
must happen in such a way that the state changes are replayed the next the
aggregate is loaded. EventFlow has a :ref:`set of different approaches <aggregates_applying_events>`
that you can select from, but in this example we use the `Apply` methods as
they are the simplest.

The ``ExampleAggrenate`` exposes the ``SetMagicNumer(int)`` method, which
is used to expose the business rules for changing the magic number. If the
magic number hasn't been set before, the event ``ExampleEvent`` is emitted
and the aggregate state is mutated.

.. IMPORTANT::
    The ``Apply(ExampleEvent)`` is invoked by the ``Emit(...)`` method, so
    after the event has been emitted, the aggregate state has changed.


Event
-----

Next up is the event which represents some that **has** happend in our domain.
In this example, its merely that some magic number has been set. Normally
these events should have a really, really good name and represent something in the
ubiquitous language for the domain.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleEvent.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 30-41

We have applied the ``[EventVersion("example", 1)]`` to our event, marking it
as the ``example`` event version ``1``, which directly corresponds to the
``event_name`` and ``event_version`` from the meta data store along side the
event mentioned. The information is used by EventFlow to tie name and version to
a specific .NET type.

.. IMPORTANT::
    Even though the using the ``EventVersion`` attribute is optional, its
    **highly recommended**. EventFlow will infer the information if it isn't
    provided and thus making it vulnerable to e.g. type renames.


Command
-------

Commands are the entry point to the domain and if you remember from the example
application, they are published using the ``ICommandBus`` as shown here.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleTests.cs
  :linenos:
  :dedent: 16
  :language: c#
  :lines: 57-62

In EventFlow commands are simple value objects that merely how the arguments for
the command execution. All commands implement the ``ICommand<,>`` interface, but
EventFlow provides an easy-to-use base class that you can use.

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleCommand.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 29-42



Command handler
---------------

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleCommandHandler.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 31-43


Read model
----------

.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleReadModel.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 30-43




Next steps
----------

Although the implementation in this guide enables you to create a complete
application, there are several topics that are recommended as next steps.

-  Use :ref:`value objects <value-objects>` to produce cleaner JSON
-  If your application need to act on an emitted event, create a
   :ref:`subscriber <subscribers>`
-  Setup a persistent event store using e.g.
   :ref:`Microsoft SQL Server <eventstore-mssql>`
-  Create :ref:`read models <read-stores>` for efficient querying








This guide describes how to get started using EventFlow.

Implementation notes

-  Aggregates and events are post fixed with ``Aggregate`` and
   ``Event``, its not required by EventFlow, but it makes it a bit
   easier to read the guide and distinguish the different types
-  ``.ConfigureAwait(false)`` is omitted to make the code easier to read
-  Make sure to read the comments about how this code should be improved
   at the bottom

Create an aggregate
-------------------

Initially you need to create the object representing the *identity* of a
user. Will use the class provided by EventFlow to help us to get
started.

.. code-block:: c#

    public class UserId : Identity<UserId>
    {
      public UserId(string value) : base(value) { }
    }


Next, let us start by creating a aggregate to represent our users.

.. code-block:: c#

    public class UserAggregate : AggregateRoot<UserAggregate, UserId>
    {
      public UserAggregate(UserId id)
        : base(id)
      {
      }
    }

Create event
------------

.. code-block:: c#

    public class UserCreatedEvent : AggregateEvent<UserAggregate, UserId>
    {
      public string Username { get; }
      public string Password { get; }

      public UserCreatedEvent(
        string username,
        string password)
      {
        Username = username;
        Password = password;
      }
    }

.. IMPORTANT::

    Once have aggregates in your production environment that have emitted
    a event, you should never change it. You can deprecate it, but you
    should never change the data stored in the event store


Update aggregate
----------------

We update our aggregate by creating a new method called ``Create(...)``
that takes the username and password and emits the ``UserCreatedEvent``
if there's no domain errors.

We also create the ``Apply(UserCreatedEvent e)`` method than applies the
event to the aggregate root.

Note that there are alternatives to applying events using ``Apply(...)``
methods, have a look at the :ref:`aggregate documentation <aggregates>`
for further details.

.. code-block:: c#

    public class UserAggregate : AggregateRoot<UserAggregate, UserId>,
      IEmit<UserCreatedEvent>
    {
      public string Username { get; private set; }
      public string Password { get; private set; }

      public UserAggregate(UserId id)
        : base(id)
      {
      }

      public void Create(
        string username,
        string password)
      {
        if (!IsNew)
        {
          // If the aggregate isn't new, i.e., events have already
          // been fired for this aggregate, then we have a domain error
          throw DomainError.With("User already created");
        }

        // Everything is okay and thus we emit the event
        Emit(new UserCreatedEvent(username, password));
      }

      public void Apply(UserCreatedEvent e)
      {
        // We must ONLY make state changes in Apply methods as anywhere
        // else will not be persisted
        Username = e.Username;
        Password = e.Password;
      }
    }

Create command
--------------

Even though it is possible, we are not allowed to call the newly created
``Create`` method on our ``UserAggregate``. The call must be made from a
command handler, and thus we first create the command.

.. code-block:: c#

    public class UserCreateCommand : Command<UserAggregate, UserId>
    {
      public string Username { get; }
      public string Password { get; }

      public UserCreateCommand(
        UserId id,
        string username,
        string password)
        : base(id)
      {
        Username = username;
        Password = password;
      }
    }

.. NOTE::
    You should read the article regarding
    :ref:`commands <commands>` for more details, e.g. on ensuring
    idempotency in a distributed application.


Create command handler
----------------------

Next we create the command handler that invokes the aggregate with the
command arguments.

.. code-block:: c#

    public class UserCreateCommand :
      CommandHandler<UserAggregate, UserId, UserCreateCommand>
    {
      public Task ExecuteAsync(
        UserAggregate aggregate,
        UserCreateCommand command,
        CancellationToken cancellationToken)
      {
        aggregate.Create(command.Username, command.Password);
        return Task.FromResult(0);
      }
    }

Create a new user
-----------------

Now all there is let is to create the user somewhere in your application
by publishing the command.

.. code-block:: c#

    var userId = UserId.New;
    var username = GetUserEnteredUsername();
    var password = GetUserEnteredPassword();

    var command = new UserCreateCommand(
      userid,
      username,
      password);

    await _commandBus.PublishAsync(command, cancellationToken);
