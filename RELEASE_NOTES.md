### New in 0.13 (not released yet)

 * _Nothing yet_

### New in 0.12.891 (released 2015-09-04)

 * Breaking: Aggregate root no longer have `Aggregate` removed from their
   when name, i.e., the metadata property with key `aggregate_name` (or
   `MetadataKeys.AggregateName`). If you are dependent on the previous naming,
   use the new `AggregateName` attribute and apply it to your aggregates
 * Breaking: Moved `Identity<>` and `IIdentity` from the `EventFlow.Aggregates`
   namespace to `EventFlow.Core` as the identities are not specific for aggregates
 * Breaking: `ICommand.Id` is renamed to `ICommand.AggregateId` to make "room"
   for the new `ICommand.SourceId` property. If commands are serialized, then
   it _might_ be important verify that the serialization still works. EventFlow
   _does not_ serialize commands, so no mitigation is provided. If the
   `Command<,>` is used, make sure to use the correct protected constructor
 * Breaking: `IEventStore.StoreAsync(...)` now requires an additional
   `ISourceId` argument. To create a random one, use `SourceId.New`, but it
   should be e.g. the command ID that resulted in the events. Note, this method
   isn't typically used by developers
 * New: Added `ICommand.SourceId`, which contains the ID of the source. The
   default (if your commands inherit from `Command<,>`) will be a new
   `CommandId` each time the a `Command<,>` instance is created. You can pass
   specific value, merely use the newly added constructor taking the ID.
   Alternatively you commands could inherit from the new
   `DistinctCommand`, enabling commands with the same state to have the
   same `SourceId`
 * New: Duplicate commands can be detected using the new `ISourceId`. Read the
   EventFlow article regarding commands for more details
 * New: Aggregate names can now be configured using the attribute
   `AggregateName`. The name can be accessed using the new `IAggregateRoot.Name`
   property
 * New: Added `Identity<>.NewDeterministic(Guid, string)` enabling creation of
   [deterministic GUIDs](http://code.logos.com/blog/2011/04/generating_a_deterministic_guid.html)
 * New: Added new metadata key `source_id` (`MetadataKeys.SourceId`) containing
   the source ID, typically the ID of the command from which the event
   originated
 * New: Added new metadata key `event_id` (`MetadataKeys.EventId`) containing a
   deterministic ID for the event. Events with the same aggregate sequence
   number and from aggregates with the same identity, will have the same event
   identity
 * Fixed: `Identity<>.With(string)` now throws an `ArgumentException` instead of
   a `TargetInvocationException` when passed an invalid identity
 * Fixed: Aggregate roots now build the cache of `Apply` methods once, instead
   of when the method is requested the first time

### New in 0.11.751 (released 2015-08-24)

 * Breaking: `EventFlowOptions.AddDefaults(...)` now also adds event
   definitions
 * New: [RabbitMQ](http://www.rabbitmq.com/) is now supported through the new
   NuGet package called `EventFlow.RabbitMQ` which enables domain events to be
   published to the bus
 * New: If you want to subscribe to all domain events, you can implement
   and register a service that implements `ISubscribeSynchronousToAll`. Services
   that implement this will automatically be added using the
   `AddSubscribers(...)` or `AddDefaults(...)` extension to `EventFlowOptions`
 * New: Use `EventFlowOptions.UseAutofacAggregateRootFactory(...)` to use an
   Autofac aggregate root factory, enabling you to use services in your
   aggregate root constructor
 * New: Use `EventFlowOptions.UseResolverAggregateRootFactory()` to use the
   resolver to create aggregate roots. Same as
   `UseAutofacAggregateRootFactory(...)` but for when using the internal IoC
   container
 * New: Use `EventFlowOptions.AddAggregateRoots(...)` to register aggregate root
   types
 * New: Use `IServiceRegistration.RegisterType(...)` to register services by
   type

### New in 0.10.642 (released 2015-08-17)

 * Breaking: Updated NuGet reference `Newtonsoft.Json` to v7.0.1
   (up from v6.0.8)
 * Breaking: Remove the empty constructor from `SingleValueObject<>`
 * New: Added `SingleValueObjectConverter` to help create clean JSON when
   e.g. domain events are serialized
 * New: Added a protected method `Register(IEventApplier)` to
   `AggregateRoot<,>` that enables developers to override how events are
   applied. Use this to e.g. implement state objects
 * New: Create `AggregateState<,,>` that developers can use to create aggregate
   state objects. Call `Register(...)` with the state object as argument
   to redirect events to it
 * New: Allow `AggregateRoot<,>.Apply(...)`, i.e., methods for applying events,
   to be `private` and `protected`
 * New: Made `AggregateRoot<,>.Emit(...)` protected and virtual to allow
   overrides that e.g. add a standard set of metadata from the aggregate state.
 * New: Made `AggregateRoot<,>.ApplyEvent(...)` protected and virtual to
   allow more custom implementations of applying events to the aggregate root.
 * Fixed: Updated internal NuGet reference `Dapper` to v1.42 (up from v1.38)

### New in 0.9.580 (released 2015-07-20)

 * Braking: `IEventStore.LoadAllEventsAsync` and `IEventStore.LoadAllEvents`
   now take a `GlobalPosition` as an argument instead of a `long` for the
   starting position. The `GlobalPosition` is basically a wrapper around a
   string that hides the inner workings of each event store.
 * New: NuGet package `EventFlow.EventStores.EventStore` that provides
   integration to [Event Store](https://geteventstore.com/). Its an initial
   version and shouldn't be used in production.

### New in 0.8.560 (released 2015-05-29)

 * Breaking: Remove _all_ functionality related to global sequence
   numbers as it proved problematic to maintain. It also matches this
   quote:

   > Order is only assured per a handler within an aggregate root
   > boundary. There is no assurance of order between handlers or
   > between aggregates. Trying to provide those things leads to
   > the dark side.
   >> Greg Young

   - If you use a MSSQL read store, be sure to delete the
     `LastGlobalSequenceNumber` column during update, or set it to
     default `NULL`
   - `IDomainEvent.GlobalSequenceNumber` removed
   - `IEventStore.LoadEventsAsync` and `IEventStore.LoadEvents` taking
     a `GlobalSequenceNumberRange` removed
 * Breaking: Remove the concept of event caches. If you really need this
   then implement it by registering a decorator for `IEventStore`
 * Breaking: Moved `IDomainEvent.BatchId` to metadata and created
   `MetadataKeys.BatchId` to help access it
 * New: `IEventStore.DeleteAggregateAsync` to delete an entire aggregate
   stream. Please consider carefully if you really want to use it. Storage
   might be cheaper than the historic knowledge within your events
 * New: `IReadModelPopulator` is new and enables you to both purge and
   populate read models by going though the entire event store. Currently
   its only basic functionality, but more will be added
 * New: `IEventStore` now has `LoadAllEventsAsync` and `LoadAllEvents` that
   enables you to load all events in the event store a few at a time.
 * New: `IMetadata.TimestampEpoch` contains the Unix timestamp version
   of `IMetadata.Timestamp`. Also, an additional metadata key
   `timestamp_epoch` is added to events containing the same data. Note,
   the `TimestampEpoch` on `IMetadata` handles cases in which the
   `timestamp_epoch` is not present by using the existing timestamp
 * Fixed: `AggregateRoot<>` now reads the aggregate version from
   domain events applied during aggregate load. This resolves an issue
   for when an `IEventUpgrader` removed events from the event stream
 * Fixed: `InMemoryReadModelStore<,>` is now thread safe

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
