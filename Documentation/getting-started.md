---
layout: default
title: Getting started
nav_order: 1
---


# Getting started

Initializing EventFlow always starts with an `EventFlowOptions.New` as this
performs the initial bootstrap and starts the fluent configuration API. The
very minimum initialization of EventFlow can be done in a single line, but
wouldn't serve any purpose as no domain has been configured.

```csharp
var resolver = EventFlowOptions.New.CreateResolver();
```

The above line does configures several important defaults

- Custom internal IoC container
- In-memory [event store](event-stores.md)
- Console logger
- A "null" snapshot store, that merely writes a warning if used (no need to
  do anything before going to production if you aren't planning to use
  snapshots)
- And lastly, default implementations of all the internal parts of EventFlow

**IMPORTANT:**
If you're using ASP.NET Core, you should install the ***EventFlow.AspNetCore*** package and invoke
`AddAspNetCoreMetadataProviders` in Startup.


```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(ef =>
  {
    ef.AddDefaults(typeof(Startup).Assembly);
    ef.AddAspNetCoreMetadataProviders();
  });
}
```

!!! attention
    Before using EventFlow in a production environment, you should configure an
    alternative **event store**, an alternative **IoC container** and another
    **logger** that sends log messages to your production log store.

    - [IoC container](../additional/customize.md)
    - [Log](../additional/customize.md)
    - [Event store](event-stores.md)
    - [Snapshots](additional/snapshots.md)

To start using EventFlow, a domain must be configured which consists of the
following parts

- [Aggregate](basics/aggregates.md)
- [Aggregate identity](basics/identity.md)
- [Aggregate events](basics/aggregates.md)
- [Commands and command handlers](basics/commands.md) (optional, but highly recommended)

In addition to the above, EventFlow provides several optional features. Whether
or not these features are utilized depends on the application in which
EventFlow is used.

- [Read models](integration/read-stores.md)
- [Subscribers](basics/subscribers.md)
- [Event upgraders](basics/event-upgrade.md)
- [Queries](basics/queries.md)
- [Jobs](basics/jobs.md)
- [Snapshots](additional/snapshots.md)
- [Sagas](basics/sagas.md)
- [Metadata providers](basics/metadata.md)

## Example application

The example application includes one of each of the required parts: aggregate,
event, aggregate identity, command and a command handler. Further down we will
go through each of the individual parts.

!!! info
    The example code provided here is located within the EventFlow code base
    exactly as shown, so if you would like to debug and step through the
    entire flow, checkout the code and execute the ``GettingStartedExample``
    test.
    
    https://github.com/eventflow/Documentation/tree/master/Source/EventFlow.Documentation/GettingStarted


All classes create for the example application are prefixed with ``Example``.

```csharp
public class ExampleTests
{
  [Test]
  public async Task ExampleTest()
  {
    // Arrange
    var resolver = EventFlowOptions.New
      .AddDefaults(typeof(ExampleAggregate).Assembly)
      .UseInMemoryReadStoreFor<ExampleReadModel>()
      .CreateResolver();

    var commandBus = resolver.Resolve<ICommandBus>();
    var queryProcessor = resolver.Resolve<IQueryProcessor>();
    var exampleId = ExampleId.New;

    // Act
    await commandBus.PublishAsync(new ExampleCommand(exampleId, 42), CancellationToken.None)
      .ConfigureAwait(false);
    var exampleReadModel = await queryProcessor.ProcessAsync(new ReadModelByIdQuery<ExampleReadModel>(exampleId), CancellationToken.None)
      .ConfigureAwait(false);

    // Assert
    exampleReadModel.MagicNumber.Should().Be(42);
  }
}
```

The above example publishes the ``ExampleCommand`` to the aggregate with the
``exampleId`` identity with the magical value of ``42``. After the command has
been published, the accompanying read model ``ExampleReadModel`` is fetched
and we verify that the magical number has reached it.

During the execution of the example application, a single event is emitted and
stored in the in-memory event store. The JSON for the event is shown here.

```json
{
  "MagicNumber": 42
}
```

The event data itself is straightforward as it is merely the JSON serialization of
an instance of the type ``ExampleEvent`` with the value we defined. A bit more
interesting is the metadata that EventFlow stores alongside the event, which is
used by the EventFlow event store.


```json
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
```

All the built-in metadata is available on each instance of ``IDomainEvent<,,>``,
which is accessible from event handlers for e.g. read models or subscribers. It is
also possible to create your own [metadata providers](basics/metadata.md)
or add additional EventFlow built-in providers as needed.


## Aggregate identity

The aggregate ID in EventFlow is represented as a value object that inherits
from the ``IIdentity`` interface. You can provide your own implementation, but
EventFlow provides a convenient implementation that will suit most needs.  Be
sure to read the section about the [Identity\<\>](basics/identity.md) class
for details on how to use it.

For our example application we use the built-in class, which makes the implementation
very simple.

```csharp
public class ExampleId : Identity<ExampleId>
{
  public ExampleId(string value) : base(value) { }
}
```

Aggregate
---------

Now we'll take a look at the ``ExampleAggregate``. It is rather simple as the
only thing it can do is apply the magic number once.

```csharp
public class ExampleAggregate : AggregateRoot<ExampleAggregate, ExampleId>,
  IEmit<ExampleEvent>
{
  private int? _magicNumber;

  public ExampleAggregate(ExampleId id) : base(id) { }

  public IExecutionResult SetMagicNumber(int magicNumber)
  {
    if (_magicNumber.HasValue)
    {
      return ExecutionResult.Failed("Magic number already set");
    }

    Emit(new ExampleEvent(magicNumber));
    return ExecutionResult.Success();
  }

  public void Apply(ExampleEvent aggregateEvent)
  {
    _magicNumber = aggregateEvent.MagicNumber;
  }
}
```

Be sure to read the section on [aggregates](basics/aggregates.md) to get all the
details right. For now the most important thing to note, is that the state
of the aggregate (updating the ``_magicNumber`` variable) happens in the
``Apply(ExampleEvent)`` method. This is the event sourcing part of EventFlow in
effect. As state changes are only saved as events, mutating the aggregate state
must happen in such a way that the state changes are replayed the next time the
aggregate is loaded. EventFlow has a [set of different approaches](basics/aggregates.md)
that you can select from. In this example we use the `Apply` methods as
they are the simplest.

!!! attention
    The ``Apply(ExampleEvent)`` is invoked by the ``Emit(...)`` method, so
    after the event has been emitted, the aggregate state has changed.

The ``ExampleAggregate`` exposes the ``SetMagicNumer(int)`` method, which
is used to expose the business rules for changing the magic number. If the
magic number hasn't been set before, the event ``ExampleEvent`` is emitted
and the aggregate state is mutated.

If the magic numer was changed, we return a failed ``IExecutionResult`` with
an error message. Returning a failed execution result will make EventFlow
disregard any events the aggregate has emitted.

If you need to return something more useful than a ``bool`` in an execution
result, merely create a new class that implements the ``IExecutionResult``
interface and specific the type as generic arguments for the command and
command handler.

!!! note
    While possible, do not use the execution results as a method of reading
    values from the aggregate, that's what the ``IQueryProcessor`` and
    read models are for.


## Event

Next up is the event which represents something that **has** happened in our domain.
In this example, it's merely that some magic number has been set. Normally
these events should have a really, really good name and represent something in the
ubiquitous language for the domain.

```csharp
[EventVersion("example", 1)]
public class ExampleEvent : AggregateEvent<ExampleAggregate, ExampleId>
{
  public ExampleEvent(int magicNumber)
  {
    MagicNumber = magicNumber;
  }

  public int MagicNumber { get; }
}
```

We have applied the ``[EventVersion("example", 1)]`` to our event, marking it
as the ``example`` event version ``1``, which directly corresponds to the
``event_name`` and ``event_version`` from the metadata store along side the
event mentioned. The information is used by EventFlow to tie the name and version to
a specific .NET type.

!!! attention
    Even though the using the ``EventVersion`` attribute is optional, it is
    **highly recommended**. EventFlow will infer the information if it isn't
    provided, thus making it vulnerable to type renames among other things.

!!! attention
    Once you have aggregates in your production environment that have emitted
    an event, you should never change the .NET implementation. You can deprecate
    it, but you should never change the type or the data stored in the event
    store.


## Command

Commands are the entry point to the domain and if you remember from the example
application, they are published using the ``ICommandBus`` as shown here.

```csharp
await commandBus.PublishAsync(new ExampleCommand(exampleId, 42), CancellationToken.None)
  .ConfigureAwait(false);
```

In EventFlow commands are simple value objects that merely house the arguments for
the command execution. All commands implement the ``ICommand<,>`` interface, but
EventFlow provides an easy-to-use base class that you can use.

```csharp
public class ExampleCommand : Command<ExampleAggregate, ExampleId>
{
  public ExampleCommand(ExampleId aggregateId, int magicNumber)
    : base(aggregateId)
  {
    MagicNumber = magicNumber;
  }

  public int MagicNumber { get; }
}
```

A command doesn't do anything without a command handler. In fact, EventFlow
will throw an exception if a command doesn't have exactly **one** command
handler registered.


## Command handler

The command handler provides the glue between the command, the aggregate and
the IoC container as it defines how a command is executed. Typically they are
rather simple, but they could contain more complex logic. How much is up to you.

```csharp
public class ExampleCommandHandler : CommandHandler<ExampleAggregate, ExampleId, ExampleCommand>
{
  public override Task<IExecutionResult> ExecuteCommandAsync(
    ExampleAggregate aggregate,
    ExampleCommand command,
    CancellationToken cancellationToken)
  {
    return Task.FromResult(aggregate.SetMagicNumber(command.MagicNumber));
  }
}
```

The ``ExampleCommandHandler`` in our case here merely invokes the
``SetMagicNumer`` on the aggregate and returns the execution result. Remember, if
a command handler returns a failed execution result, EventFlow will disregard any
events the aggregate has emitted.

!!! attention
    Everything inside the ``ExecuteAsync(...)`` method of a command handler
    **may** be executed more than once if there's an optimistic concurrency
    exception, i.e., something else has happened to the aggregate since it
    as loaded from the event store and its therefor automatically reloaded by
    EventFlow. It is therefor essential that the command handler doesn't mutate
    anything other than the aggregate.


## Read model

If you ever need to access the data in your aggregates efficiently, its important
that [read models](integration/read-stores.md) are used. Loading aggregates from the
event store takes time and its impossible to query for e.g. aggregates that have
a specific value in its state.

In our example we merely use the built-in in-memory read model store. It is useful
in many cases, e.g. executing automated domain tests in a CI build.

```csharp
public class ExampleReadModel : IReadModel,
  IAmReadModelFor<ExampleAggregate, ExampleId, ExampleEvent>
{
  public int MagicNumber { get; private set; }

  public void Apply(
    IReadModelContext context,
    IDomainEvent<ExampleAggregate, ExampleId, ExampleEvent> domainEvent)
  {
    MagicNumber = domainEvent.AggregateEvent.MagicNumber;
  }
}
```

Notice the ``IDomainEvent<ExampleAggrenate, ExampleId, ExampleEvent> domainEvent``
argument. It's merely a wrapper around the specific event we implemented
earlier. The ``IDomainEvent<,,>`` provides additional information, e.g. any
metadata stored alongside the event.

The main difference between the event instance emitted in the aggregate and the
instance wrapped here, is that the event has been committed to the event store.


## Next steps

Although the implementation in this guide enables you to create a complete
application, there are several topics that are recommended as next steps.

-  Read the [dos and donts](additional/dos-and-donts.md) section
-  Use [value objects](additional/value-objects.md) to produce cleaner JSON
-  If your application need to act on an emitted event, create a
   [subscriber](basics/subscribers.md)
-  Check the [configuration](additional/configuration.md) to make sure everything
   is as you would like it
-  Setup a persistent event store using e.g.
   [Microsoft SQL Server](integration/mssql.md)
-  Create [read models](integration/read-stores.md) for efficient querying
-  Consider the use of [specifications](additional/specifications.md) to ease
   creation of business rules
