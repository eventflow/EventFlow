### New in 0.6 (not released yet)

 * Breaking: `ITransientFaultHandler` now takes the strategy as a generic
   argument instead of the `Use<>` method. If you want to configure the
   retry strategy, use `ConfigureRetryStrategy(...)` instead
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
