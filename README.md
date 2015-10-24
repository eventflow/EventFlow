# EventFlow

<table border="0" cellpadding="0" cellspacing="0">
  <tr>
    <td>
      <img src="./icon-128.png" />
    </td>
    <td>
      <p>
        <a href="https://gitter.im/rasmus/EventFlow?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge">
          <img src="https://badges.gitter.im/Join%20Chat.svg" />
        </a>
      </p>
      <p>
        <a href="https://www.nuget.org/packages/EventFlow/">
          <img src="http://img.shields.io/nuget/v/EventFlow.svg?style=flat" />
        </a>
      </p>
      <p>
        <a href="https://ci.appveyor.com/project/rasmusnu/eventflow">
          <img src="https://ci.appveyor.com/api/projects/status/51yvhvbd909e4o82/branch/develop?svg=true" />
        </a>
      </p>
    </td>
  </tr>
</table>

EventFlow is a basic CQRS+ES framework designed to be easy to use.

Have a look at our [getting started guide](./Documentation/GettingStarted.md),
the [dos and don'ts](./Documentation/DoesAndDonts.md) and the
[FAQ](./Documentation/FAQ.md).

### Features

* **CQRS+ES framework**
* **Async/await first:** Every part of EventFlow is written using async/await.
* **Highly configurable and extendable**
* **Easy to use**
* **No use of threads or background workers making it "web friendly"**
* **Cancellation:** All methods that does IO work or might delay execution (due to
  retries), takes a `CancellationToken` argument to allow you to cancel the operation

### Examples

* **[Simple](#simple example):** Shows the key concepts of EventFlow in a few
  lines of code
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

* [**Aggregates:**](./Documentation/Aggregates.md) Domains object
  that guarantees the consistency of changes being made within
  each aggregate
* [**Command bus and commands:**](./Documentation/Commands.md)
  Entry point for all command/operation execution.
* **Event store:** Storage of the event stream for aggregates.
  Currently there is support for these storage types.
 * In-memory - only for test
 * Files - only for test
 * [Microsoft SQL Server](./Documentation/EventStores-MSSQL.md)
 * EventStore - only for test (for now) [home page](https://geteventstore.com/)
* **Read models:** Denormalized representation of aggregate events
  optimized for reading fast. Currently there is support for these
  read model storage types.
  * [Elasticsearch](./Documentation/ReadStores-Elasticsearch.md)
  * In-memory - only for test
  * Microsoft SQL Server
* [**Queries:**](./Documentation/Queries.md) Value objects that represent
  a query without specifying how its executed, that is let to a query handler
* [**Jobs:**](./Documentation/Jobs.md) Perform scheduled tasks at a later time,
  e.g. publish a command. EventFlow provides support for these job schedulers
  * [Hangfire](./Documentation/Jobs.md#hangfire) - [home page](http://hangfire.io/)
* [**Event upgrade:**](./Documentation/EventUpgrade.md) As events committed to
  the event store is never changed, EventFlow uses the concept of event
  upgraders to deprecate events and replace them with new during aggregate load.
* **Event publishing:** Sometimes you want other applications or services to
  consume and act on domains. For this EventFlow supports event publishing.
 * [RabbitMQ](./Documentation/RabbitMQ.md)
* [**Metadata:**](./Documentation/Metadata.md)
  Additional information for each aggregate event, e.g. the IP of
  the user behind the event being emitted. EventFlow ships with
  several providers ready to use used.
* [**Value objects:**](./Documentation/ValueObjects.md) Data containing classes
  used to validate and hold domain data, e.g. a username or e-mail.
* [**Customize:**](./Documentation/Customize.md) Almost every single part of
  EventFlow can be swapped with a custom implementation through the embedded
  IoC container.

## Simple example
Here's an example on how to use the in-memory event store (default)
and a in-memory read model store.

```csharp
using (var resolver = EventFlowOptions.New
    .AddEvents(typeof (TestAggregate).Assembly)
    .AddCommandHandlers(typeof (TestAggregate).Assembly)
    .UseInMemoryReadStoreFor<TestAggregate, TestReadModel>()
    .CreateResolver())
{
  var commandBus = resolver.Resolve<ICommandBus>();
  var eventStore = resolver.Resolve<IEventStore>();
  var readModelStore = resolver.Resolve<IInMemoryReadModelStore<
    TestAggregate,
    TestReadModel>>();
  var id = TestId.New;

  // Publish a command
  await commandBus.PublishAsync(new PingCommand(id));

  // Load aggregate
  var testAggregate = await eventStore.LoadAggregateAsync<TestAggregate>(id);

  // Get read model from in-memory read store
  var testReadModel = await readModelStore.GetAsync(id);
}
```

Note: `.ConfigureAwait(false)` omitted in above example.

## State of EventFlow

EventFlow is still under development, especially the parts regarding
how read models are re-populated.

EventFlow  _is_ currently used in production environments and performs very well,
but it need to mature before key APIs are stable.

EventFlow is greatly opinionated, but its possible to create new implementations
for almost every part of EventFlow by registering a different implementation of a
a interface.

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
 - [How To Ensure Idempotency In An Eventual Consistent DDD/CQRS Application](http://blog.sapiensworks.com/post/2015/08/26/How-To-Ensure-Idempotency/)
   by Mike Mogosanu
* **Why _not_ to implement "unit of work" in DDD**
 - [Unit Of Work is the new Singleton](http://blog.sapiensworks.com/post/2014/06/04/Unit-Of-Work-is-the-new-Singleton.aspx/)
   by Mike Mogosanu
 - [The Unit of Work and Transactions In Domain Driven Design](http://blog.sapiensworks.com/post/2015/09/02/DDD-and-UoW/)
   by Mike Mogosanu

## How to contribute

EventFlow still needs a lot of love and if you want to help out there are
several areas that you could help out with.

* **Features:** If you have a great idea for EventFlow, create a pull request.
   It might be a finished idea or just some basic concepts showing the feature
   outline
* **Pull request feedback:** Typically there are several pull requests marked
   with the `in progress` and feedback is always welcome. Please note that the
   quality of the code here might not be "production ready", especially if
   the pull request is marked with the `prof of concept` label
* **Documentation:** Good documentation is very important for any library and
   is also very hard to do properly, so if spot a spelling error, think up
   a good idea for a guide or just have some comments, then please create
   either a pull request or an issue
* **Information sharing:** Working with CQRS+ES and DDD is hard, so if you come
   across articles that might be relevant for EventFlow, or even better, can
   point to specfic EventFlow functionality that might be done better, then
   please create an issue or ask in the Gitter chat
* **Expand the shipping example:** If you have ideas on how to expand the
  shipping example found in the code base, the please create a pull request
  or create an issue
  * Give a good understanding of how to use EventFlow
  * Give a better understanding of how API changes in EventFlow affect
    existing applications
  * Provide a platform for DDD discussions

## Thanks

[![ReSharper](./Documentation/Images/logo_resharper.png)](https://www.jetbrains.com/resharper/)


## License

```
The MIT License (MIT)

Copyright (c) 2015 Rasmus Mikkelsen
https://github.com/rasmus/EventFlow

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
