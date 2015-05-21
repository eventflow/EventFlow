# EventFlow

[![NuGet Status](http://img.shields.io/nuget/v/EventFlow.svg?style=flat)](https://www.nuget.org/packages/EventFlow/)
[![Build status](https://ci.appveyor.com/api/projects/status/51yvhvbd909e4o82/branch/develop?svg=true)](https://ci.appveyor.com/project/rasmusnu/eventflow)
[![License](https://img.shields.io/github/license/rasmus/eventflow.svg)](./LICENSE)

EventFlow is a basic CQRS+ES framework designed to be easy to use.

Have a look at our [Getting started guide](./Documentation/GettingStarted.md).

### Features

* CQRS+ES framework
* Async/await first
* Highly configurable and extendable
* Easy to use
* No use of threads or background workers making it "web friendly"
* Cancellation

### Overview

Here is a list of the EventFlow concepts. Use the links to navigate
to the documentation.

* [**Aggregates:**](./Documentation/Aggregates.md) Domains object
  that guarantees the consistency of changes being made within
  each aggregate
* **Command bus:** Entry point for all command execution.
* **Event store:** Storage of the event stream for aggregates.
  Currently there is support for these storage types.
 * In-memory - only for test
 * Files - only for test
 * [Microsoft SQL Server](./Documentation/ReadStores-MSSQL.md)
* **Read models:** Denormalized representation of aggregate events
  optimized for reading fast. Currently there is support for these
  read model storage types.
  * In-memory - only for test
  * Microsoft SQL Server
* [**Event upgrade**](./Documentation/EventUpgrade.md): As events committed to
  the event store is never changed, EventFlow uses the concept of event upgraders
  to deprecate events and replace them with new during aggregate load.
* [**Metadata:**](./Documentation/Metadata.md)
  Additional information for each aggregate event, e.g. the IP of
  the user behind the event being emitted. EventFlow ships with
  several providers ready to use used.

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
