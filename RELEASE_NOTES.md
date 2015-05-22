### New in 0.8 (not released yet)

 * _Nothing yet_

### New in 0.7.481 (released 2015-05-22)

 * New: EventFlow now includes a `IQueryProcessor` that enables you to implement
   queries and query handlers in a structure manner. EventFlow ships with two
   ready-to-use queries and related handlers
   - `ReadModelByIdQuery<TReadModel>`: Supported by in-memory and MSSQL read
     model stores
   - `InMemoryQuery<TReadModel>`: Only supported by in-memory read model store,
     but lets you search for any read model based on a `Predicate<TReadModel>`

### New in 0.6.456 (released 2015-05-18)

 * Breaking: Read models have been significantly improved as they can now
   subscribe to events from multiple aggregates. Use a custom
   `IReadModelLocator` to define how read models are located. The supplied
   `ILocateByAggregateId` simply uses the aggregate ID. To subscribe
   to other events, simply implement `IAmReadModelFor<,,>` and make sure
   you have supplied a proper read model locator.
   - `UseMssqlReadModel` signature changed, change to
   `.UseMssqlReadModel<MyReadModel, ILocateByAggregateId>()` in
   order to have the previous functionality
   - `UseInMemoryReadStoreFor` signature changed, change to
   `.UseInMemoryReadStoreFor<MyReadModel, ILocateByAggregateId>()` in
   order to have the previous functionality
 * Breaking: A warning is no longer logged if you forgot to subscribe to
   a aggregate event in your read model as read models are no longer
   strongly coupled to a specific aggregate and its events
 * Breaking: `ITransientFaultHandler` now takes the strategy as a generic
   argument instead of the `Use<>` method. If you want to configure the
   retry strategy, use `ConfigureRetryStrategy(...)` instead
 * New: You can now have multiple `IReadStoreManager` if you would like to
   implement your own read model handling
 * New: `IEventStore` now has a `LoadEventsAsync` and `LoadEvents`
   that loads `IDomainEvent`s based on global sequence number range
 * New: Its now possible to register generic services without them being
   constructed generic types, i.e., register `typeof(IMyService<>)` as
   `typeof(MyService<>)`
 * New: Table names for MSSQL read models can be assigned using the
   `TableAttribute` from `System.ComponentModel.DataAnnotations`
 * Fixed: Subscribers are invoked _after_ read stores have been updated,
   which ensures that subscribers can use any read models that were
   updated

### New in 0.5.390 (released 2015-05-08)

 * POTENTIAL DATA LOSS for files event store: Files event store now
   stores its log as JSON instead of an `int` in the form
   `{"GlobalSequenceNumber":2}`. So rename the current file and put in the
   global sequence number before startup
 * Breaking: Major changes has been made regarding how the aggregate
   identity is implemented and referenced through interfaces. These changes makes
   it possible to access the identity type directly though all interface. Some
   notable examples are listed here. Note that this has NO impact on how data
   is stored!
   - `IAggregateRoot` changed to `IAggregateRoot<TIdentity>`
   - `ICommand<TAggregate>` changed to `ICommand<TAggregate,TIdentity>`
   - `ICommandHandler<TAggregate,TCommand>` changed to
     `ICommandHandler<TAggregate,TIdentity, TCommand>`
   - `IAmReadModelFor<TEvent>` changed to
     `IAmReadModelFor<TAggregate,TIdentity,TEvent>`
   - `IDomainEvent<TEvent>` changed to `IDomainEvent<TAggregate,TIdentity>`  
 * New: `ICommandBus.Publish` now takes a `CancellationToken` argument
 * Fixed: MSSQL should list columns to SELECT when fetching events


### New in 0.4.353 (released 2015-05-05)

* Breaking: `ValueObject` now uses public properties instead of both
  private and public fields
* Breaking: Aggregate IDs are no longer `string` but objects implementing
  `IIdentity`
* Breaking: MSSQL transient exceptions are now retried
* Breaking: All methods on `IMsSqlConnection` has an extra `Label` argument
* New: `ITransientFaultHandler` added along with default retry strategies
  for optimistic concurrency and MSSQL transient exceptions
* New: Release notes added to NuGet packages
* New: Better logging and more descriptive exceptions
* Fixed: Unchecked missing in `ValueObject` when claculating hash
* Fixed: `NullReferenceException` thrown if `null` was stored
  in `SingleValueObject` and `ToString()` was called


### New in 0.3.292 (released 2015-04-30)

* First stable version of EventFlow
