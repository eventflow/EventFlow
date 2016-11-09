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
- A "null" snapshot store, that merely writes a warning if used
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



.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleTests.cs
  :linenos:
  :dedent: 12
  :language: c#
  :lines: 41-74


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleId.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 29-33

.. NOTE::
    Be sure to read the read the section about the
    :ref:`Identity\<\> <identity>` class to get details on how to use it.


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleAggrenate.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 30-54


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleEvent.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 30-40


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleCommand.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 29-41


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleCommandHandler.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 31-42


.. literalinclude:: ../Source/EventFlow.Tests/Documentation/GettingStarted/ExampleReadModel.cs
  :linenos:
  :dedent: 4
  :language: c#
  :lines: 31-42












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

Improvements
------------

There are several areas the code can be improved.

-  Use :ref:`value objects <value-objects>` for e.g. username and
   password that validate the value, i.e., ensure that the username
   isn't the empty string
-  If your application need to act on the emitted ``UserCreatedEvent``,
   create a :ref:`subscriber <subscribers>`
-  Setup a persistent event store using e.g.
   :ref:`Microsoft SQL Server <eventstore-mssql>`
-  Create :ref:`read models <read-stores>` for efficient querying
