# EventFlow

<table border="0" cellpadding="0" cellspacing="0">
  <tr>
    <td width="25%">
      <img src="./icon-128.png" />
    </td>
    <td  width="25%">
      <p>
        <a href="https://gitter.im/rasmus/EventFlow?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge"><img src="https://badges.gitter.im/Join%20Chat.svg" /></a>
      </p>
      <p>
        <a href="https://www.nuget.org/packages/EventFlow/"><img src="http://img.shields.io/nuget/v/EventFlow.svg?style=flat" /></a>
      </p>
      <p>
        <a href="http://docs.geteventflow.net/?badge=latest"><img src="https://readthedocs.org/projects/eventflow/badge/?version=latest" /></a>
      </p>
    </td>
    <td  width="25%">
      <p>
        <a href="https://ci.appveyor.com/project/rasmusnu/eventflow"><img src="https://ci.appveyor.com/api/projects/status/51yvhvbd909e4o82/branch/develop?svg=true" /></a>
      </p>
      <p>
        <a href="https://codecov.io/github/eventflow/EventFlow?branch=develop"><img src="https://codecov.io/github/eventflow/EventFlow/coverage.svg?branch=develop" /></a>
      </p>
    </td>
    <td  width="25%">
      Think EventFlow is great,<br/>
      <a href="https://www.paypal.me/rasmusnu">buy me a cup of coffee</a>
    </td>
  </tr>
</table>

EventFlow is a basic CQRS+ES framework designed to be easy to use.

Have a look at our [getting started guide](http://docs.geteventflow.net/GettingStarted.html),
the [do’s and don’ts](http://docs.geteventflow.net/DosAndDonts.html) and the
[FAQ](http://docs.geteventflow.net/FAQ.html).

### Features

* **CQRS+ES framework**
* **Async/await first:** Every part of EventFlow is written using async/await.
* **Highly configurable and extendable**
* **Easy to use**
* **No use of threads or background workers making it "web friendly"**
* **Cancellation:** All methods that does IO work or might delay execution (due to
  retries), takes a `CancellationToken` argument to allow you to cancel the operation

### Examples

* **[Complete](#complete-example):** Shows a complete example on how to use
  EventFlow with in-memory event store and read models in a relatively few lines
  of code
* **Shipping:** To get a more complete example of how EventFlow _could_ be used,
  have a look at the shipping example found here in the code base. The example
  is based on the shipping example from the book "Domain-Driven Design -
  Tackling Complexity in the Heart of Software" by Eric Evans. Its
  _in-progress_, but should provide inspiration on how to use EventFlow on a
  larger scale. If you have ideas and/or comments, create a pull request or
  an issue

### Overview

Here is a list of the EventFlow concepts. Use the links to navigate
to the documentation.

* [**Aggregates:**](http://docs.geteventflow.net/Aggregates.html)
  Domains object that guarantees the consistency of changes being made within
  each aggregate
* [**Command bus and commands:**](http://docs.geteventflow.net/Commands.html)
  Entry point for all command/operation execution.
* [**Event store:**](http://docs.geteventflow.net/EventStore.html)
  Storage of the event stream for aggregates. Currently there is support for
  these storage types.
 * In-memory - only for test
 * Files - only for test
 * Microsoft SQL Server
 * EventStore - only for test (for now) [home page](https://geteventstore.com/)
* [**Subscribers:**](http://docs.geteventflow.net/Subscribers.html)
  Listeners that act on specific domain events. Useful if an specific action
  needs to be triggered after a domain event has been committed.
* [**Read models:**](http://docs.geteventflow.net/ReadStores.html)
  Denormalized representation  of aggregate events optimized for reading fast.
  Currently there is support for these read model storage types.
  * [Elasticsearch](http://docs.geteventflow.net/ReadStores.html#elasticsearch)
  * [In-memory](http://docs.geteventflow.net/ReadStores.html#in-memory) - only for test
  * [Microsoft SQL Server](http://docs.geteventflow.net/ReadStores.html#microsoft-sql-server)
* [**Snapshots:**](http://docs.geteventflow.net/Snapshots.html)
  Instead of reading the entire event stream every single time, a snapshot can
  be created every so often that contains the aggregate state. EventFlow
  supports upgrading existing snapshots, which is useful for long-lived
  aggregates. Snapshots in EventFlow are opt-in and EventFlow has support for
  * [In-memory](http://docs.geteventflow.net/Snapshots.html#in-memory) - only for test
  * [Microsoft SQL Server](http://docs.geteventflow.net/Snapshots.html#microsoft-sql-server)  
* [**Sagas:**](http://docs.geteventflow.net/Sagas.html)
  Also known as _process managers_, coordinates and routes messages between
  bounded contexts and aggregates
* [**Queries:**](http://docs.geteventflow.net/Queries.html)
  Value objects that represent a query without specifying how its executed,
  that is let to a query handler
* [**Jobs:**](http://docs.geteventflow.net/Jobs.html) Perform scheduled tasks at
  a later time, e.g. publish a command. EventFlow provides support for these
  job schedulers
  * [Hangfire](http://docs.geteventflow.net/Jobs.html#hangfire) - [home page](http://hangfire.io/)
* [**Event upgrade:**](http://docs.geteventflow.net/EventUpgrade.html)
  As events committed to the event store is never changed, EventFlow uses the
  concept of event upgraders to deprecate events and replace them with new
  during aggregate load.
* **Event publishing:** Sometimes you want other applications or services to
  consume and act on domains. For this EventFlow supports event publishing.
 * [RabbitMQ](http://docs.geteventflow.net/Subscribers.html#rabbitmq)
* [**Metadata:**](http://docs.geteventflow.net/Metadata.html)
  Additional information for each aggregate event, e.g. the IP of
  the user behind the event being emitted. EventFlow ships with
  several providers ready to use used.
* [**Value objects:**](http://docs.geteventflow.net/ValueObjects.html)
  Data containing classes used to validate and hold domain data, e.g. a
  username or e-mail.
* [**Customize:**](http://docs.geteventflow.net/Customize.html) Almost every
  single part of EventFlow can be swapped with a custom implementation through
  the embedded IoC container.

## Complete example
Here's a complete example on how to use the default in-memory event store
along with an in-memory read model.

The example consists of the following classes, each shown below

- `ExampleAggregate`: The aggregate root
- `ExampleId`: Value object representing the identity of the aggregate root
- `ExampleEvent`: Event emitted by the aggregate root
- `ExampleCommand`: Value object defining a command that can be published to the
  aggregate root
- `ExampleCommandHandler`: Command handler which EventFlow resolves using its IoC
  container and defines how the command specific is applied to the aggregate root
- `ExampleReadModel`: In-memory read model providing easy access to the current
  state

**Note:** This example is part of the EventFlow test suite, so checkout the
code and give it a go.

```csharp
[Test]
public async Task Example()
{
  // We wire up EventFlow with all of our classes. Instead of adding events,
  // commands, etc. explicitly, we could have used the the simpler
  // AddDefaults(Assembly) instead.
  using (var resolver = EventFlowOptions.New
    .AddEvents(typeof(ExampleEvent))
    .AddCommands(typeof(ExampleCommand))
    .AddCommandHandlers(typeof(ExampleCommandHandler))
    .UseInMemoryReadStoreFor<ExampleReadModel>()
    .CreateResolver())
  {
    // Create a new identity for our aggregate root
    var exampleId = ExampleId.New;

    // Resolve the command bus and use it to publish a command
    var commandBus = resolver.Resolve<ICommandBus>();
    await commandBus.PublishAsync(
      new ExampleCommand(exampleId, 42), CancellationToken.None)
      .ConfigureAwait(false);

    // Resolve the query handler and use the built-in query for fetching
    // read models by identity to get our read model representing the
    // state of our aggregate root
    var queryProcessor = resolver.Resolve<IQueryProcessor>();
    var exampleReadModel = await queryProcessor.ProcessAsync(
      new ReadModelByIdQuery<ExampleReadModel>(exampleId), CancellationToken.None)
      .ConfigureAwait(false);

    // Verify that the read model has the expected magic number
    exampleReadModel.MagicNumber.Should().Be(42);
  }
}
```

```csharp
// The aggregate root
public class ExampleAggrenate : AggregateRoot<ExampleAggrenate, ExampleId>,
  IEmit<ExampleEvent>
{
  private int? _magicNumber;

  public ExampleAggrenate(ExampleId id) : base(id) { }

  // Method invoked by our command
  public void SetMagicNumer(int magicNumber)
  {
    if (_magicNumber.HasValue)
      throw DomainError.With("Magic number already set");

    Emit(new ExampleEvent(magicNumber));
  }

  // We apply the event as part of the event sourcing system. EventFlow
  // provides several different methods for doing this, e.g. state objects,
  // the Apply method is merely the simplest
  public void Apply(ExampleEvent aggregateEvent)
  {
    _magicNumber = aggregateEvent.MagicNumber;
  }
}
```

```csharp
// Represents the aggregate identity (ID)
public class ExampleId : Identity<ExampleId>
{
  public ExampleId(string value) : base(value) { }
}
```

```csharp
// A basic event containing some information
public class ExampleEvent : AggregateEvent<ExampleAggrenate, ExampleId>
{
  public ExampleEvent(int magicNumber)
  {
      MagicNumber = magicNumber;
  }

  public int MagicNumber { get; }
}
```

```csharp
// Command for update magic number
public class ExampleCommand : Command<ExampleAggrenate, ExampleId>
{
  public ExampleCommand(
    ExampleId aggregateId,
    int magicNumber)
    : base(aggregateId)
  {
    MagicNumber = magicNumber;
  }

  public int MagicNumber { get; }
}
```

```csharp
// Command handler for our command
public class ExampleCommandHandler
  : CommandHandler<ExampleAggrenate, ExampleId, ExampleCommand>
{
  public override Task ExecuteAsync(
    ExampleAggrenate aggregate,
    ExampleCommand command,
    CancellationToken cancellationToken)
  {
    aggregate.SetMagicNumer(command.MagicNumber);
    return Task.FromResult(0);
  }
}
```

```csharp
// Read model for our aggregate
public class ExampleReadModel : IReadModel,
  IAmReadModelFor<ExampleAggrenate, ExampleId, ExampleEvent>
{
  public int MagicNumber { get; private set; }

  public void Apply(
    IReadModelContext context,
    IDomainEvent<ExampleAggrenate, ExampleId, ExampleEvent> domainEvent)
  {
    MagicNumber = domainEvent.AggregateEvent.MagicNumber;
  }
}
```


## State of EventFlow

EventFlow is still under development, especially the parts regarding
how read models are re-populated.

EventFlow  _is_ currently used in production environments and performs very well,
but it need to mature before key APIs are stable.

EventFlow is greatly opinionated, but its possible to create new implementations
for almost every part of EventFlow by registering a different implementation of
an interface.

## Useful articles related to EventFlow and DDD

Many of the technical design decisions in EventFlow is based on articles. This
section lists some of them. If you have a link with a relevant article, please
share it by creating an issue with the link.

* **Domain-Driven Design**
 - [Domain-Driven Design Reference](https://domainlanguage.com/ddd/reference/) by Eric Evans
* **General CQRS+ES**
 - [CQRS Journey by Microsoft](https://msdn.microsoft.com/en-us/library/jj554200.aspx)
   published by Microsoft
 - [An In-Depth Look At CQRS](http://blog.sapiensworks.com/post/2015/09/01/In-Depth-CQRS/)
   by Mike Mogosanu
 - [CQRS, Task Based UIs, Event Sourcing agh!](http://codebetter.com/gregyoung/2010/02/16/cqrs-task-based-uis-event-sourcing-agh/)
   by Greg Young
 - [Busting some CQRS myths](https://lostechies.com/jimmybogard/2012/08/22/busting-some-cqrs-myths/)
   by Jimmy Bogard
 - [CQRS applied](https://lostechies.com/gabrielschenker/2015/04/12/cqrs-applied/)
   by Gabriel Schenker
* **Eventual consistency**
 - [How To Ensure Idempotency In An Eventual Consistent DDD/CQRS Application](http://blog.sapiensworks.com/post/2015/08/26/How-To-Ensure-Idempotency)
   by Mike Mogosanu
* **Why _not_ to implement "unit of work" in DDD**
 - [Unit Of Work is the new Singleton](http://blog.sapiensworks.com/post/2014/06/04/Unit-Of-Work-is-the-new-Singleton.aspx)
   by Mike Mogosanu
 - [The Unit of Work and Transactions In Domain Driven Design](http://blog.sapiensworks.com/post/2015/09/02/DDD-and-UoW/)
   by Mike Mogosanu


### Integration tests
EventFlow has several tests that verify that its able to use the systems it
integrates with correctly.

 * **Elasticsearch:** [Elasticsearch](https://www.elastic.co/) is automatically
   downloaded and run during the Elasticsearch integration tests from your `TEMP`
   directory. Requires Java to be installed and the `JAVA_HOME` environment
   variable set
 * **EventStore:** [EventStore](https://geteventstore.com/) is automatically
   downloaded and run during the EventStore integration tests from your `TEMP`
   directory
 * **MSSQL:** Microsoft SQL Server is required to be running
 * **RabbitMQ:** Set an environment variable named `RABBITMQ_URL` with the URL
   for the [RabbitMQ](https://www.rabbitmq.com/) instance you would like to use.

There's a Vagrant box with both Elasticsearch and RabbitMQ you can use
[here](https://github.com/rasmus/Vagrant.Boxes).

Alternatively you can skip the NUnit tests marked with the `integration`
category.

## Thanks

<table border="0" cellpadding="0" cellspacing="0">
  <tr>
    <td width="25%">
      <a href="https://www.jetbrains.com/"><img src="./Resources/jetbrains-128x128.png" /></a>
    </td>
  </tr>
</table>

* [Contributors](https://github.com/eventflow/EventFlow/graphs/contributors)
* [JetBrains](https://www.jetbrains.com/resharper/): OSS licenses
* [olholm](https://github.com/olholm): Current logo
* [iconmonstr](http://iconmonstr.com/network-6-icon/): First logo
* [JC008](https://github.com/JC008): License for Navicat Essentials for SQLite

## License

```
The MIT License (MIT)

Copyright (c) 2015-2017 Rasmus Mikkelsen
Copyright (c) 2015-2017 eBay Software Foundation
https://github.com/eventflow/EventFlow

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
