# EventFlow

<table border="0" cellpadding="0" cellspacing="0">
  <tr>
    <td width="25%">
      <img src="./icon-128.png" />
    </td>
    <td  width="25%">
      <p>
        <a href="https://www.nuget.org/packages/EventFlow/"><img src="https://img.shields.io/nuget/v/EventFlow.svg?style=flat" /></a>
      </p>
      <p>
        <a href="https://docs.geteventflow.net/?badge=latest"><img src="https://readthedocs.org/projects/eventflow/badge/?version=latest" /></a>
      </p>
    </td>
    <td  width="25%">
      <p>
        <a href="https://github.com/eventflow/EventFlow/actions/workflows/build.yml"><img src="https://github.com/eventflow/EventFlow/actions/workflows/build.yml/badge.svg" /></a>
      </p>
      <p>
        <a href="https://gitter.im/rasmus/EventFlow?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge"><img src="https://badges.gitter.im/Join%20Chat.svg" /></a>
      </p>
     <!--
      <p>
        <a href="https://codecov.io/github/eventflow/EventFlow?branch=develop"><img src="https://codecov.io/github/eventflow/EventFlow/coverage.svg?branch=develop" /></a>
      </p>
      -->
    </td>
  </tr>
</table>

NuGet feeds
- Official releases: https://www.nuget.org/packages/EventFlow/
- Builds: https://ci.appveyor.com/nuget/eventflow

EventFlow is a basic CQRS+ES framework designed to be easy to use.

Have a look at our [getting started guide](https://docs.geteventflow.net/GettingStarted.html),
the [do’s and don’ts](https://docs.geteventflow.net/DosAndDonts.html) and the
[FAQ](https://docs.geteventflow.net/FAQ.html).

## Features

* **Easy to use**: Designed with sensible defaults and implementations that make it
  easy to create an example application
* **Highly configurable and extendable**: EventFlow uses interfaces for every part of
  its core, making it easy to replace or extend existing features with custom
  implementation
* **No use of threads or background workers**
* **MIT licensed** Easy to understand and use license for enterprise

## Versions

Development of version 1.0 has started and is mainly braking changes regarding changes
related to replacing EventFlow types with that of Microsoft extension abstractions,
mainly `IServiceProvider` and `ILogger<>`.

The following list key characteristics of each version as well as its related branches
(not properly configured yet).

* `1.x` (under development, not all projects compile yet)
  
  Represents the next iteration of EventFlow that aligns EventFlow with the standard
  packages for .NET (Core). Releases here will only support .NET Standard, .NET Core
  and .NET versions going forward.

  Read the [migration guide](./MIGRATION_GUIDE.md) to view the full list of breaking
  changes as well as recommendations on how to migrate.

  **NOTE:** This version is under heavy development and NO stable version has yet
  been releases. It will take a few alpha/beta/rc releases to get this right. These
  will be released as soon as any significant changes have been made.

  NuGet package status (🟢 compiles and pushed in `-alpha` releases, 🔴 not yet
  ported to 1.0 and 💀 for packages that are removed as part of 1.0, see the
  [migration guide](./MIGRATION_GUIDE.md) for details)
  - 🟢 `EventFlow`
  - 🔴 `EventFlow.AspNetCore`
  - 💀 `EventFlow.Autofac`
  - 💀 `EventFlow.DependencyInjection`
  - 🔴 `EventFlow.Elasticsearch`
  - 🔴 `EventFlow.EntityFramework`
  - 🔴 `EventFlow.EventStores.EventStore`
  - 🔴 `EventFlow.Hangfire`
  - 🔴 `EventFlow.MongoDB`
  - 🟢 `EventFlow.MsSql`
  - 💀 `EventFlow.Owin`
  - 🔴 `EventFlow.PostgreSql`
  - 🔴 `EventFlow.RabbitMQ`
  - 🟢 `EventFlow.Sql`
  - 🔴 `EventFlow.SQLite`
  - 🟢 `EventFlow.TestHelpers`

  Branches:
  - `develop-v1`: Development branch, pull requests should be done here
  - `release-v1`: Release branch, merge commits are done to this branch from
    `develop-v1` to create releases. Typically each commit represents a release

* `0.x` (API stable)

  The current stable version of EventFlow and has been the version of EventFlow
  for almost six years. 0.x versions have .NET Framework support and limited
  support to the Microsoft extension packages through extra NuGet packages.

  Feature and bug fix releases will still be done while there's interest in
  the community.

  Branches:
  - `develop-v0`: Development branch, pull requests should be done here
  - `release-v0`: Release branch, merge commits are done to this branch from
    `develop-v0` to create releases. Typically each commit represents a release


## Examples

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
  
### External Examples

List of examples create by different community members. Note that many of these
examples will be using EventFlow 0.x.

*Create a pull request to get your exampled linked from here.*

 * **[Racetimes:](https://github.com/dennisfabri/Eventflow.Example.Racetimes)**
   Shows some features of EventFlow that are not covered in the 
   [complete example](#complete-example). It features entities, a read model for 
   an entity, delete on read models, specifications and snapshots.

   * **[Racetimes for Azure Functions:](https://github.com/craignicol/Eventflow.Example.Racetimes)**
     Extends the above example to support the HTTP access via Azure Functions
     
   * **[Racetimes for Azure Functions and Event Grid:](https://github.com/craignicol/Eventflow.Example.Racetimes/tree/feature/event-grid-as-extension)**
     Further extends the Azure Functions Example to publish to Event Grid, following the RabbitMQ pattern

 * **[.NET Core:](https://github.com/johnny-chan/EventFlowDemo)**
	A Web API running .NET Core 2.2 using the event flow. It uses the pre-defined 
	command/entities/events from the [complete example](#complete-example). There are endpoints to 
	create a new example event, getting a data model and to replay all data models.
	
* **[ElasticSearch/.NET Core:](https://github.com/DureSameen/EventFlowWithElasticSearch)**
	It is configured with EventFlow, ElasticSearch, EventStore, and RabbitMq. See "withRabbitMq" branch for #384.

 * **[Vehicle Tracking:](https://github.com/MongkonEiadon/VehicleTracker)**
	A Microservice on .NET Core 2.2 with docker based, you can up the service with docker-compose, this project using various
  tools to up the services aka. Linux Docker based on .NET Core, RabbitMq, EntityFramework with SQL Server and using EventFlow following CQRS-ES architecture
  and all microservice can access through ApiGateway which using Ocelot

  * **[RestAirline:](https://github.com/twzhangyang/RestAirline)**
	A classic DDD with CQRS-ES, Hypermedia API project based on EventFlow. It's targeted to ASP.NET Core 2.2 and can be deployed to docker and k8s.
	
* **[Full Example:](https://github.com/OKTAYKIR/EventFlow.Example)**
	A console application on .NET Core 2.2. You can up the services using [docker-compose file](https://github.com/OKTAYKIR/EventFlow.Example/blob/master/build/docker-compose.yml). Docker-compose file include EventStore, RabbitMq, MongoDb, and PostgreSQL. It include following EventFlow concepts:
	* Aggregates
	* Command bus and commands
	* Synchronous subscriber
	* Event store ([GES](https://eventstore.com/))
	* In-memory read model.
	* Snapshots ([MongoDb](https://www.mongodb.com/))
	* Sagas
	* Event publising (In-memory, [RabbitMq](https://www.rabbitmq.com/))
	* Metadata
	* Command bus decorator, custom value object, custom execution result, ...
	
### Overview

Here is a list of the EventFlow concepts. Use the links to navigate
to the documentation.

* [**Aggregates:**](https://docs.geteventflow.net/Aggregates.html)
  Domains object that guarantees the consistency of changes being made within
  each aggregate
* [**Command bus and commands:**](https://docs.geteventflow.net/Commands.html)
  Entry point for all command/operation execution.
* [**Event store:**](https://docs.geteventflow.net/EventStore.html)
  Storage of the event stream for aggregates. Currently there is support for
  these storage types.
  * In-memory - only for test
  * Files - only for test
  * Microsoft SQL Server
  * Entity Framework Core
  * SQLite
  * PostgreSQL
  * EventStore - [home page](https://eventstore.org/)
* [**Subscribers:**](https://docs.geteventflow.net/Subscribers.html)
  Listeners that act on specific domain events. Useful if an specific action
  needs to be triggered after a domain event has been committed.
* [**Read models:**](https://docs.geteventflow.net/ReadStores.html)
  Denormalized representation of aggregate events optimized for reading fast.
  Currently there is support for these read model storage types.
  For the SQL storage types the queries are being generated automatically with quoted columns and table names.
  * [Elasticsearch](https://docs.geteventflow.net/ReadStores.html#elasticsearch)
  * [In-memory](https//docs.geteventflow.net/ReadStores.html#in-memory) - only for test
  * [Microsoft SQL Server](https://docs.geteventflow.net/ReadStores.html#microsoft-sql-server)
  * Entity Framework Core
  * SQLite
  * PostgreSQL
* [**Snapshots:**](https://docs.geteventflow.net/Snapshots.html)
  Instead of reading the entire event stream every single time, a snapshot can
  be created every so often that contains the aggregate state. EventFlow
  supports upgrading existing snapshots, which is useful for long-lived
  aggregates. Snapshots in EventFlow are opt-in and EventFlow has support for
  * [In-memory](https://docs.geteventflow.net/Snapshots.html#in-memory) - only for test
  * [Microsoft SQL Server](https://docs.geteventflow.net/Snapshots.html#microsoft-sql-server)  
  * Entity Framework Core
  * SQLite
  * PostgreSQL
* [**Sagas:**](https://docs.geteventflow.net/Sagas.html)
  Also known as _process managers_, coordinates and routes messages between
  bounded contexts and aggregates
* [**Queries:**](https://docs.geteventflow.net/Queries.html)
  Value objects that represent a query without specifying how its executed,
  that is let to a query handler
* [**Jobs:**](https://docs.geteventflow.net/Jobs.html) Perform scheduled tasks at
  a later time, e.g. publish a command. EventFlow provides support for these
  job schedulers
  * [Hangfire](https://docs.geteventflow.net/Jobs.html#hangfire) - [home page](https://hangfire.io/)
* [**Event upgrade:**](https://docs.geteventflow.net/EventUpgrade.html)
  As events committed to the event store is never changed, EventFlow uses the
  concept of event upgraders to deprecate events and replace them with new
  during aggregate load.
* **Event publishing:** Sometimes you want other applications or services to
  consume and act on domains. For this EventFlow supports event publishing.
  * [RabbitMQ](https://docs.geteventflow.net/Subscribers.html#rabbitmq)
* [**Metadata:**](https://docs.geteventflow.net/Metadata.html)
  Additional information for each aggregate event, e.g. the IP of
  the user behind the event being emitted. EventFlow ships with
  several providers ready to use used.
* [**Value objects:**](https://docs.geteventflow.net/ValueObjects.html)
  Data containing classes used to validate and hold domain data, e.g. a
  username or e-mail.
* [**Customize:**](https://docs.geteventflow.net/Customize.html) Almost every
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
  var serviceCollection = new ServiceCollection()
    .AddLogging()
    .AddEventFlow(o => o
      .AddEvents(typeof(ExampleEvent))
      .AddCommands(typeof(ExampleCommand))
      .AddCommandHandlers(typeof(ExampleCommandHandler))
      .UseInMemoryReadStoreFor<ExampleReadModel>());

  using (var serviceProvider = serviceCollection.BuildServiceProvider())
  {
    // Create a new identity for our aggregate root
    var exampleId = ExampleId.New;

    // Resolve the command bus and use it to publish a command
    var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
    await commandBus.PublishAsync(
      new ExampleCommand(exampleId, 42), CancellationToken.None);

    // Resolve the query handler and use the built-in query for fetching
    // read models by identity to get our read model representing the
    // state of our aggregate root
    var queryProcessor = serviceProvider.GetRequiredService<IQueryProcessor>();
    var exampleReadModel = await queryProcessor.ProcessAsync(
      new ReadModelByIdQuery<ExampleReadModel>(exampleId), CancellationToken.None);

    // Verify that the read model has the expected magic number
    exampleReadModel.MagicNumber.Should().Be(42);
  }
}
```

```csharp
// The aggregate root
public class ExampleAggregate : AggregateRoot<ExampleAggregate, ExampleId>,
  IEmit<ExampleEvent>
{
  private int? _magicNumber;

  public ExampleAggregate(ExampleId id) : base(id) { }

  // Method invoked by our command
  public void SetMagicNumber(int magicNumber)
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
public class ExampleEvent : AggregateEvent<ExampleAggregate, ExampleId>
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
public class ExampleCommand : Command<ExampleAggregate, ExampleId>
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
  : CommandHandler<ExampleAggregate, ExampleId, ExampleCommand>
{
  public override Task ExecuteAsync(
    ExampleAggregate aggregate,
    ExampleCommand command,
    CancellationToken cancellationToken)
  {
    aggregate.SetMagicNumber(command.MagicNumber);
    return Task.CompletedTask;;
  }
}
```

```csharp
// Read model for our aggregate
public class ExampleReadModel : IReadModel,
  IAmReadModelFor<ExampleAggregate, ExampleId, ExampleEvent>
{
  public int MagicNumber { get; private set; }

  public Task ApplyAsync(
    IReadModelContext context,
    IDomainEvent<ExampleAggregate, ExampleId, ExampleEvent> domainEvent,
    CancellationToken _cancellationToken
  {
    MagicNumber = domainEvent.AggregateEvent.MagicNumber;
    return Task.CompletedTask;
  }
}
```


## State of EventFlow

EventFlow is still under development, especially the parts regarding
how read models are re-populated.

EventFlow  _is_ currently used in production environments and performs very well,
but it needs to mature before key APIs are stable.

EventFlow is greatly opinionated, but it's possible to create new implementations
for almost every part of EventFlow by registering a different implementation of
an interface.

## Useful articles related to EventFlow and DDD

Many of the technical design decisions in EventFlow is based on articles. This
section lists some of them. If you have a link with a relevant article, please
share it by creating an issue with the link.

* **Domain-Driven Design**
  * [Domain-Driven Design Reference](https://domainlanguage.com/ddd/reference/)
    by Eric Evans
  * [DDD Decoded - Bounded Contexts Explained](https://blog.sapiensworks.com/post/2016/08/12/DDD-Bounded-Contexts-Explained)
  * [Going "Events-First" for Microservices with Event Storming and DDD](http://www.russmiles.com/essais/going-events-first-for-microservices-with-event-storming-and-ddd)
* **General CQRS+ES**
  * [CQRS Journey by Microsoft](https://msdn.microsoft.com/en-us/library/jj554200.aspx)
    published by Microsoft
  * [An In-Depth Look At CQRS](https://blog.sapiensworks.com/post/2015/09/01/In-Depth-CQRS)
    by Mike Mogosanu
  * [CQRS, Task Based UIs, Event Sourcing agh!](http://codebetter.com/gregyoung/2010/02/16/cqrs-task-based-uis-event-sourcing-agh/)
    by Greg Young
  * [Busting some CQRS myths](https://lostechies.com/jimmybogard/2012/08/22/busting-some-cqrs-myths/)
    by Jimmy Bogard
  * [CQRS applied](https://lostechies.com/gabrielschenker/2015/04/12/cqrs-applied/)
    by Gabriel Schenker
  * [DDD Decoded - Entities and Value Objects Explained](https://blog.sapiensworks.com/post/2016/07/29/DDD-Entities-Value-Objects-Explained)
* **Eventual consistency**
  * [How To Ensure Idempotency In An Eventual Consistent DDD/CQRS Application](https://blog.sapiensworks.com/post/2015/08/26/How-To-Ensure-Idempotency)
   by Mike Mogosanu
  * [DDD Decoded - Don't Fear Eventual Consistency](https://blog.sapiensworks.com/post/2016/07/23/DDD-Eventual-Consistency)
* **Why _not_ to implement "unit of work" in DDD**
  * [Unit Of Work is the new Singleton](https://blog.sapiensworks.com/post/2014/06/04/Unit-Of-Work-is-the-new-Singleton.aspx)
    by Mike Mogosanu
  * [The Unit of Work and Transactions In Domain-Driven Design](https://blog.sapiensworks.com/post/2015/09/02/DDD-and-UoW)
    by Mike Mogosanu


### Integration tests

EventFlow has several tests that verify that its ability to use the systems it
integrates with correctly.

 * [Elasticsearch](https://www.elastic.co/)
 * [EventStore](https://geteventstore.com/)
 * [RabbitMQ](https://www.rabbitmq.com/)
 * Microsoft SQL Server
 * PostgreSQL

To setup a local test environment run the following commands in the checkout
directory of EventFlow.

```
docker-compose pull
docker-compose up
```

Alternatively, you can skip the NUnit tests marked with the `integration`
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
* [iconmonstr](https://iconmonstr.com/network-6-icon/): First logo
* [JC008](https://github.com/JC008): License for Navicat Essentials for SQLite

## License

```
The MIT License (MIT)

Copyright (c) 2015-2021 Rasmus Mikkelsen
Copyright (c) 2015-2021 eBay Software Foundation
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

