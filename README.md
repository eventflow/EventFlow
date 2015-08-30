# EventFlow

[![Join the chat at https://gitter.im/rasmus/EventFlow](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/rasmus/EventFlow?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![NuGet Status](http://img.shields.io/nuget/v/EventFlow.svg?style=flat)](https://www.nuget.org/packages/EventFlow/)
[![Build status](https://ci.appveyor.com/api/projects/status/51yvhvbd909e4o82/branch/develop?svg=true)](https://ci.appveyor.com/project/rasmusnu/eventflow)
[![License](https://img.shields.io/github/license/rasmus/eventflow.svg)](./LICENSE)

EventFlow is a basic CQRS+ES framework designed to be easy to use.

Have a look at our [getting started guide](./Documentation/GettingStarted.md),
the [dos and don'ts](./Documentation/DoesAndDonts.md) and the
[FAQ](./Documentation/FAQ.md).

### Features

* **CQRS+ES framework**
* **Async/await first:** Every part of EventFlow is written using async/await. In
  some places EventFlow exposes sync methods like e.g. the `ICommandBus`, but these
  merely _try_ to do the right thing using an async bridge
* **Highly configurable and extendable**
* **Easy to use**
* **No use of threads or background workers making it "web friendly"**
* **Cancellation:** All methods that does IO work or might delay execution (due to
  retries), takes a `CancellationToken` argument to allow you to cancel the operation

### Overview

Here is a list of the EventFlow concepts. Use the links to navigate
to the documentation.

* [**Aggregates**](./Documentation/Aggregates.md): Domains object
  that guarantees the consistency of changes being made within
  each aggregate
* **Command bus:** Entry point for all command execution.
* **Event store:** Storage of the event stream for aggregates.
  Currently there is support for these storage types.
 * In-memory - only for test
 * Files - only for test
 * [Microsoft SQL Server](./Documentation/EventStores-MSSQL.md)
 * EventStore - only for test (for now) [home page](https://geteventstore.com/)
* **Read models:** Denormalized representation of aggregate events
  optimized for reading fast. Currently there is support for these
  read model storage types.
  * In-memory - only for test
  * Microsoft SQL Server
* [**Queries:**](./Documentation/Queries.md): Value objects that represent
  a query without specifying how its executed, that is let to a query handler
* [**Event upgrade:**](./Documentation/EventUpgrade.md): As events committed to
  the event store is never changed, EventFlow uses the concept of event
  upgraders to deprecate events and replace them with new during aggregate load.
* **Event publishing:** Sometimes you want other applications or services to
  consume and act on domains. For this EventFlow supports event publishing.
 * [RabbitMQ](./Documentation/RabbitMQ.md)
* [**Metadata**](./Documentation/Metadata.md):
  Additional information for each aggregate event, e.g. the IP of
  the user behind the event being emitted. EventFlow ships with
  several providers ready to use used.
* [**Value objects**](./Documentation/ValueObjects.md): Data containing classes
  used to validate and hold domain data, e.g. a username or e-mail.
* [**Performance tips:**](./Documentation/PerformanceTips.md) As EventFlow is
  general purpose, there are some areas that you can do optimizations based
  on how you use EventFlow
* [**Customize**](./Documentation/Customize.md): Almost every single part of
  EventFlow can be swapped with a custom implementation through the embedded
  IoC container.

## Full example
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

## Useful links

* [CQRS Journey by Microsoft](https://msdn.microsoft.com/en-us/library/jj554200.aspx)

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
