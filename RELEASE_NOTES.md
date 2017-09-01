### New in 0.49 (not released yet)

* Breaking: Upgraded `EventStore.Client` dependency to version 4.0
* Breaking: Changed target framework for `EventFlow.EventStores.EventStore` to
  .NET 4.6.2 as required by `EventStore.Client` NuGet dependency
* New: Added an overload to `IDomainEventPublisher.PublishAsync` that isn't
  generic and doesn't require an aggregate ID
* New: Added `IReadModelPopulator.DeleteAsync` that allows deletion of single
  read models
* Obsolete: `IDomainEventPublisher.PublishAsync<,>` (generic) in favor of the
  new less restrictive non-generic overload

### New in 0.48.2937 (released 2017-07-11)

* Breaking: Moved non-async methods on `IReadModelPopulator` to extension
  methods
* New: Added non-generic overloads for purge and populate methods on
  `IReadModelPopulator`
* New: Provided `EventFlow.TestHelpers` which contains several test suites
  that is useful when developing event and read model stores for EventFlow.
  The package is an initial release and its interface is unstable and
  subject to change
* New: Now possible to configure retry delay for MSSQL error `40501` (server
  too busy) using `IMsSqlConfiguration.SetServerBusyRetryDelay(RetryDelay)`
* New: Now possible to configure the retry count of transient exceptions for
  MSSQL and SQLite using the `ISqlConfiguration.SetTransientRetryCount(int)`
* Fixed: Added MSSQL error codes `10928`, `10929`, `18401` and `40540` as well
  as a few native `Win32Exception` exceptions to the list treated as transient
  errors, i.e., EventFlow will automatically retry if the server returns one
  of these

### New in 0.47.2894 (released 2017-06-28)

* New: To be more explicit, `IEventFlowOpions.AddSynchronousSubscriber<,,,>` and
  `IEventFlowOpions.AddAsynchronousSubscriber<,,,>` generic methods
* Fix: `IEventFlowOpions.AddSubscriber`, `IEventFlowOpions.AddSubscribers` and  
  `IEventFlowOpions.AddDefaults` now correctly registers implementations of
  `ISubscribeAsynchronousTo<,,>`
* Obsolete:  `IEventFlowOpions.AddSubscriber` is marked obsolete in favor of its
  explicite counterparts

### New in 0.46.2886 (released 2017-05-29)

* Fix: EventFlow now uses a Autofac lifetime scope for validating service
  registrations when `IEventFlowOpions.CreateResolver(true)` is invoked.
  Previously services were created but never disposed as they were resolved
  using the root container

### New in 0.45.2877 (released 2017-05-28)

* Breaking: Asynchronous subscribers are now **disabled by default**, i.e.,
  any implementations of `ISubscribeAsynchronousTo<,,>` wont get invoked
  unless enabled
  ```
  eventFlowOptions.Configure(c => IsAsynchronousSubscribersEnabled = true);
  ```
  the `ITaskRunner` has been removed and asynchronous subscribers are now
  invoked using a new scheduled job that's scheduled to run right after the
  domain events are emitted. Using the `ITaskRunner` led to unexpected task
  terminations, especially if EventFlow was hosted in IIS. If enabling
  asynchronous subscribers, please _make sure_ to configure proper job
  scheduling, e.g. by using the `EventFlow.Hangfire` NuGet package. The default
  job scheduler is `InstantJobScheduler`, which executes jobs _synchronously_,
  giving a end result similar to that of synchronous subscribers
* Breaking: `InstantJobScheduler`, the default in-memory scheduler if nothing
  is configured, now swallows all job exceptions and logs them as errors. This
  ensure that the `InstantJobScheduler` behaves as any other out-of-process
  job scheduler

### New in 0.44.2832 (released 2017-05-12)

* New: .NET Standard 1.6 support for the following NuGet packages
  - `EventFlow`
  - `EventFlow.Autofac`
  - `EventFlow.Elasticsearch`
  - `EventFlow.Hangfire`
  - `EventFlow.RabbitMQ`
* Fixed: Removed dependency `Microsoft.Owin.Host.HttpListener` from
  `EventFlow.Owin` as it doesn't need it

### New in 0.43.2806 (released 2017-05-05)

* Breaking: Updated _all_ NuGet package dependencies to their latest versions
* New: EventFlow now embeds PDB and source code within the assemblies using
  [SourceLink](https://github.com/ctaggart/SourceLink) (GitLink now removed)

### New in 0.42.2755 (released 2017-05-02)

* Fixed: The deterministic `IDomainEvent.Metadata.EventId` is now correctly
  based on the both the aggregate identity and the aggregate sequence number,
  instead of merely the aggregate identity
* Fixed: [GitLink](https://github.com/gittools/gitlink) PDB source URLs

### New in 0.41.2727 (released 2017-04-27)

* New: NuGet packages now contain PDB files with links to GitHub
  (thanks to [GitLink](https://github.com/gittools/gitlink)). Be sure
  to check `Enable source server support` to be able to step through
  the EventFlow source code. See GitLink documentation for details
* Fixed: Fixed a bug in how EventFlow registers singletons with Autofac
  that made Autofac invoke `IDisposable.Dispose()` upon disposing
  lifetime scopes

### New in 0.40.2590 (released 2017-03-30)

* New: Updated EventFlow logo (thanks @olholm)
* Fixed: Corrected logo path in NuGet packages

### New in 0.39.2553 (released 2017-01-16)

* New: Autofac is no longer IL merged into the `EventFlow` core NuGet package.
  This is both in preparation for .NET Core and to simplify the build process.
  EventFlow now ships with a custom IoC container by default. The Autofac based
  IoC container is still available via the `EventFlow.Autofac` and will
  continue to be supported as it is recommended for production use
* New: An IoC container based aggregate root factory is now the default
  aggregate factory. The old implementation merely invoked a constructor
  with the aggregate ID as argument. The new default also checks if any
  additional services are required for the constructor making the distinction
  between the two obsolete
* New: `Command<,,>` now inherits from `ValueObject`
* Obsolete: `UseResolverAggregateRootFactory()` and `UseAutofacAggregateRootFactory()`
  are marked as obsolete as this is now the default. The current implementation
  of these methods does nothing
* Obsolete: All `IEventFlowOptions.AddAggregateRoots(...)` overloads are obsolete,
  the aggregate factory no longer has any need for the aggregate types to be
  registered with the container. The current implementation of the method does
  nothing

### New in 0.38.2454 (released 2016-12-02)

* Fix: Single aggregate read models can now be re-populated again

### New in 0.37.2424 (released 2016-11-08)

* Breaking: Remove the following empty and deprecated MSSQL NuGet packages. If
  you use any of these packages, then switch to the `EventFlow.MsSql` package
  - `EventFlow.EventStores.MsSql`
  - `EventFlow.ReadStores.MsSql`
* Breaking: `ITaskRunner.Run(...)` has changed signature. The task factory now
  gets an instance of `IResolver` that is valid for the duration of the task
  execution
* Fixed: The resolver scope of `ISubscribeAsynchronousTo<,,>` is now valid for
  the duration of the domain handling
* New: Documentation is now released in HTML format along with NuGet packages.
  Access the ZIP file from the GitHub releases page

### New in 0.36.2315 (released 2016-10-18)

* New: Documentation is now hosted at http://docs.geteventflow.net/ and
  http://eventflow.readthedocs.io/ and while documentation is still kept
  along the source code, the documentation files have been converted from
  markdown to reStructuredText
* New: Added `ISubscribeAsynchronousTo<,,>` as an alternative to the existing
  `ISubscribeSynchronousTo<,,>`, which allow domain event subscribers to be
  executed using the new `ITaskRunner`
* New: Added `ITaskRunner` for which the default implementation is mere a thin
  wrapper around `Task.Run(...)` with some logging added. Implemting this
  interface allows control of how EventFlows runs tasks. Please note that
  EventFlow will only use `ITaskRunner` in very limited cases, e.g. if
  there's implantations of `ISubscribeAsynchronousTo<,,>`

### New in 0.35.2247 (released 2016-09-06)

* Fixed: `IAggregateStore.UpdateAsync` and `StoreAsync` now publishes committed
  events as expected. This basically means that its now possible to circumvent the
  command and command handler pattern and use the `IAggregateStore.UpdateAsync`
  directly to modify an aggregate root
* Fixed: Domain events emitted from aggregate sagas are now published

### New in 0.34.2221 (released 2016-08-23)

* **New core feature:** EventFlow now support sagas, also known as process
  managers. The use of sagas is opt-in. Currently EventFlow only supports sagas
  based on aggregate roots, but its possible to implement a custom saga store.
  Consult the documentation for details on how to get started using sagas
* New: Added `IMemoryCache` for which the default implementation is a thin
  wrapper for the .NET built-in `MemoryCache`. EventFlow relies on extensive use
  of reflection and the internal parts of EventFlow will move to this
  implementation for caching internal reflection results to allow better control
  of EventFlow memory usage. Invoke the `UsePermanentMemoryCache()` extension
  method on `IEventFlowOptions` to have EventFlow use the previous cache
  behavior using `ConcurrentDictionary<,,>` based in-memory cache
* New: Added `Identity<>.With(Guid)` which allows identities to be created
  based on a specific `Guid`
* New: Added `Identity<>.GetGuid()` which returns the internal `Guid`

### New in 0.33.2190 (released 2016-08-16)

* Fixed: Fixed regression in `v0.32.2163` by adding NuGet package reference
  `DbUp` to `EventFlow.Sql`. The package was previously ILMerged.
* Fixed: Correct NuGet package project URL
  - Old: https://github.com/rasmus/EventFlow
  - New: https://github.com/eventflow/EventFlow

### New in 0.32.2163 (released 2016-07-04)

* Breaking: This release contains several breaking changes related to
  Elasticsearch read models
  - Elasticsearch NuGet package has been renamed to `EventFlow.Elasticsearch`
  - Upgraded Elasticsearch dependencies to version 2.3.3
  - Purging all read models from Elasticsearch for a specific type now
    **deletes the index** instead of doing a _delete by query_. Make sure to
    create a separate index for each read model. Delete by query has been
    [moved to a plugin in Elasticsearch 2.x](https://www.elastic.co/blog/core-delete-by-query-is-a-plugin) and
    deleting the entire index is now recommended    
  - The default index for a read model is now `eventflow-[lower case type name]`,
    e.g. `eventflow-thingyreadmodel`, instead of merely `eventflow`
* Breaking: The following NuGet dependencies have been updated
  - `Elasticsearch.Net` v2.3.3 (up from v1.7.1)
  - `Elasticsearch.Net.JsonNET` removed
  - `NEST` v2.3.3 (up from v1.7.1)
  - `Newtonsoft.Json` v8.0.3 (up from v7.0.1)
* Breaking: Several non-async methods have been moved from the following
  interfaces to extension methods and a few additional overloads have
  been created
  - `IEventStore`
  - `ICommandBus`

### New in 0.31.2106 (released 2016-06-30)

* New: EventFlow can now be configured to throw exceptions thrown by subscribers
  by `options.Configure(c => c.ThrowSubscriberExceptions = true)`
* New: Added an `ICommandScheduler` for easy scheduling of commands

### New in 0.30.2019 (released 2016-06-16)

* Breaking: To simplify the EventFlow NuGet package structure, the two NuGet
  packages `EventFlow.EventStores.MsSql` and `EventFlow.ReadStores.MsSql` have
  been discontinued and their functionality move to the existing package
  `EventFlow.MsSql`. The embedded SQL scripts have been made idempotent making
  the upgrade a simple operation of merely using the new name spaces. To make
  the upgrade easier, the deprecated NuGet packages will still be uploaded,
  but will not contain anything
* Fixed: When configuring Elasticsearch and using the overload of
  `ConfigureElasticsearch` that takes multiple of URLs, `SniffingConnectionPool`
  is now used instead of `StaticConnectionPool` and with sniff life span of five
  minutes

### New in 0.29.1973 (released 2016-04-19)

* Breaking: `IAggregateRoot` has some breaking changes. If these methods aren't
  used (which is considered the typical case), then the base class
  `AggregateRoot<,,>` will automatically handle it
  - `CommitAsync` has an additional `ISnapshotStore` parameter. If you don't
    use snapshot aggregates, then you can safely pass `null`
  - `LoadAsync` is a new method that lets the aggregate control how its
    loaded fromt the event- and snapshot stores
* **New core feature:** EventFlow now support snapshot creation for aggregate
  roots. The EventFlow documentation has been updated to include a guide on
  how to get started using snapshots. Snapshots are basically an opt-in optimized
  method for handling long-lived aggregate roots. Snapshot support in EventFlow
  introduces several new elements, read the documentation to get an overview.
  Currently EventFlow offers the following snapshot stores
  - In-memory
  - Microsoft SQL Server
* New: The `IAggregateStore` is introduced, which provides a cleaner interface
  for manipulating aggregate roots. The most important method is the
  `UpdateAsync` which allows easy updates to aggregate roots without the need
  for a command and command handler
  - `LoadAsync`
  - `UpdateAsync`
  - `StoreAsync`
* New: `IEventStore` now supports loading events from a specific version using
  the new overload of `LoadEventsAsync` that takes a `fromEventSequenceNumber`
  argument
* New: `IMsSqlDatabaseMigrator` now has a overloaded method named
  `MigrateDatabaseUsingScripts` that takes an `IEnumerable<SqlScript>`
  enabling specific scripts to be used in a database migration
* New: Added suport to use EventStore persistence with connection strings
  instead IPs only
* Obsolete: The following aggregate related methods on `IEventStore` has been
  marked as obsolete in favor of the new `IAggregateStore`. The methods will be
  removed at some point in the future
  - `LoadAggregateAsync`
  - `LoadAggregate`

### New in 0.28.1852 (released 2016-04-05)

* **Critical fix:** `OptimisticConcurrencyRetryStrategy` now correctly only
  states that `OptimisticConcurrencyException` should be retried. Before
  _ALL_ exceptions from the event stores were retried, not only the transient!
  If you have inadvertently become dependent on this bug, then implement your
  own `IOptimisticConcurrencyRetryStrategy` that has the old behavior
* Fixed: `OptimisticConcurrencyRetryStrategy` has a off-by-one error that caused
  it to retry one less that it actually should
* Fixed: Prevent `abstract ICommandHandler<,,,>` from being registered in
   `EventFlowOptionsCommandHandlerExtensions.AddCommandHandlers(...)`
* Fixed: Prevent `abstract IEventUpgrader<,>` from being registered in
   `EventFlowOptionsEventUpgradersExtensions.AddEventUpgraders(...)`
* Fixed: Prevent `abstract IMetadataProvider` from being registered in
   `EventFlowOptionsMetadataProvidersExtensions.AddMetadataProviders(...)`
* Fixed: Prevent `abstract IQueryHandler<,>` from being registered in
   `EventFlowOptionsQueriesExtensions.AddQueryHandlers(...)`
* Fixed: Prevent `abstract ISubscribeSynchronousTo<,,>` from being registered in
   `EventFlowOptionsSubscriberExtensions.AddSubscribers(...)`

### New in 0.27.1765 (released 2016-02-25)

 * New: Configure Hangfire job display names by implementing
   `IJobDisplayNameBuilder`. The default implementation uses job description
   name and version

### New in 0.26.1714 (released 2016-02-20)

 * Breaking: Renamed `MssqlMigrationException` to `SqlMigrationException`
 * Breaking: Renamed `SqlErrorRetryStrategy` to `MsSqlErrorRetryStrategy`
   as its MSSQL specific
 * Breaking: The NuGet package `Dapper` is no longer IL merged with the package
   `EventFlow.MsSql` but is now listed as a NuGet dependency. The current
   version used by EventFlow is `v1.42`
 * New: Introduced the NuGet package `EventFlow.SQLite` that adds event store
   support for SQLite databases, both as event store and read model store
 * New: Introduced the NuGet package `EventFlow.Sql` as shared package for
   EventFlow packages that uses SQL
 * New: Its now possible to configure the retry delay for MSSQL transient
   errors using the new `IMsSqlConfiguration.SetTransientRetryDelay`. The
   default is a random delay between 50 and 100 milliseconds

### New in 0.25.1695 (released 2016-02-15)

* Fixed: Deadlock in `AsyncHelper` if e.g. an exception caused no `async` tasks
  to be scheduled. The `AsyncHelper` is used by EventFlow to expose non-`async`
  methods to developers and provide the means to call `async` methods from
  a synchronous context without causing a deadlock. There's no change to any of
  the `async` methods.

  The `AsyncHelper` is used in the following methods.
  - `ICommandBus.Publish`
  - `IEventStore.LoadEvents`
  - `IEventStore.LoadAggregate`
  - `IEventStore.LoadAllEvents`
  - `IJobRunner.Execute`
  - `IReadModelPopulator.Populate`
  - `IReadModelPopulator.Purge`
  - `IQueryProcessor.Process`

### New in 0.24.1563 (released 2016-01-17)

 * Breaking: The following NuGet references have been updated
   - `EventStore.Client` v3.4.0 (up from v3.0.2)
   - `Hangfire.Core` v1.5.3 (up from v1.4.6)
   - `RabbitMQ.Client` v3.6.0 (up from v3.5.4)
 * New: EventFlow now uses Paket to manage NuGet packages
 * Fixed: Incorrect use of `EventStore.Client` that caused it to throw
   `WrongExpectedVersionException` when committing aggregates multiple times
 * Fixed: Updated NuGet package titles of the following NuGet packages to
   contain assembly name to get a better overview when searching on
   [nuget.org](http://nuget.org)
   - `EventFlow.RabbitMQ`
   - `EventFlow.EventStores.EventStore`
 * Fixed: Updated internal NuGet reference `dbup` to v3.3.0 (up from v3.2.1)

### New in 0.23.1470 (released 2015-12-05)

* Breaking: EventFlow no longer ignores columns named `Id` in MSSQL read models.
  If you were dependent on this, use the `MsSqlReadModelIgnoreColumn` attribute
* Fixed: Instead of using `MethodInfo.Invoke` to call methods on reflected
  types, e.g. when a command is published, EventFlow now compiles an expression
  tree instead. This has a slight initial overhead, but provides a significant
  performance improvement for subsequent calls
* Fixed: Read model stores are only invoked if there's any read model updates
* Fixed: EventFlow now correctly throws an `ArgumentException` if EventFlow has
  been incorrectly configure with known versioned types, e.g. an event
  is emitted that hasn't been added during EventFlow initialization. EventFlow
  would handle the save operation correctly, but if EventFlow was reinitialized
  and the event was loaded _before_ it being emitted again, an exception would
  be thrown as EventFlow would know which type to use. Please make sure to
  correctly load all event, command and job types before use
* Fixed: `IReadModelFactory<>.CreateAsync(...)` is now correctly used in
  read store mangers
* Fixed: Versioned type naming convention now allows numbers

### New in 0.22.1393 (released 2015-11-19)

* New: To customize how a specific read model is initially created, implement
  a specific `IReadModelFactory<>` that can bootstrap that read model
* New: How EventFlow handles MSSQL read models has been refactored to allow
  significantly more freedom to developers. MSSQL read models are no longer
  required to implement `IMssqlReadModel`, only the empty `IReadModel`
  interface. Effectively, this means that no specific columns are required,
  meaning that the following columns are no longer enforced on MSSQL read
  models. Use the new required `MsSqlReadModelIdentityColumn` attribute to mark
  the identity column and the optional (but recommended)
  `MsSqlReadModelVersionColumn` to mark the version column.
  - `string AggregateId`
  - `DateTimeOffset CreateTime`
  - `DateTimeOffset UpdatedTime`
  - `int LastAggregateSequenceNumber`
* Obsolete: `IMssqlReadModel` and `MssqlReadModel`. Developers should instead
  use the `MsSqlReadModelIdentityColumn` and `MsSqlReadModelVersionColumn`
  attributes to mark the identity and version columns (read above).
  EventFlow will continue to support `IMssqlReadModel`, but it _will_ be
  removed at some point in the future
* Fixed: Added missing `UseElasticsearchReadModel<TReadModel, TReadModelLocator>()`
  extension

### New in 0.21.1312 (released 2015-10-26)

* New: Added `Identity<>.NewComb()` that creates sequential unique IDs which can
  be used to minimize database fragmentation
* New: Added `IReadModelContext.Resolver` to allow read models to fetch
  additional resources when events are applied
* New: The `PrettyPrint()` type extension method, mostly used for verbose
  logging, now prints even prettier type names, e.g.
  `KeyValuePair<Boolean,Int64>` instead of merely `KeyValuePair'2`, making log
  messages slightly more readable

### New in 0.20.1274 (released 2015-10-22)

* Breaking: `Entity<T>` now inherits from `ValueObject` but uses only the `Id`
  field as equality component. Override `GetEqualityComponents()` if you have
  a different notion of equality for a specific entity
* Breaking: `Entity<T>` will now throw an `ArgumentNullException` if the `id`
  passed to its constructor is `null`
* Breaking: Fixed method spelling. Renamed
 `ISpecification<T>.WhyIsNotStatisfiedBy` to `WhyIsNotSatisfiedBy` and
 `Specification<T>.IsNotStatisfiedBecause` to `IsNotSatisfiedBecause`
* New: Read model support for Elasticsearch via the new NuGet package
  `EventFlow.ReadStores.Elasticsearch`

### New in 0.19.1225 (released 2015-10-19)

* Breaking: `AddDefaults` now also adds the job type definition to the
  `IJobsDefinitonService`
* New: Implemented a basic specification pattern by providing
  `ISpecification<T>`, an easy-to-use `Specificaion<T>` and a set of extension
  methods. Look at the EventFlow specification tests to get started
* Fixed: `IEventDefinitionService`, `ICommandDefinitonService` and
  `IJobsDefinitonService` now longer throw an exception if an existing
  event is loaded, i.e., multiple calls to `AddEvents(...)`, `AddCommand(...)`
  and `AddJobs(...)` no longer throws an exception
* Fixed: `DomainError.With(...)` no longer executes `string.format` if only
  one argument is parsed

### New in 0.18.1181 (released 2015-10-07)

* POTENTIAL DATA LOSS for the **files event store**: The EventFlow
  internal functionality regarding event stores has been refactored resulting
  in information regarding aggregate names being removed from the event
  persistence layer. The files based event store no longer stores its events in
  the path `[STORE PATH]\[AGGREGATE NAME]\[AGGREGATE ID]\[SEQUENCE].json`, but
  in the path `[STORE PATH]\[AGGREGATE ID]\[SEQUENCE].json`. Thus if you are
  using the files event store for tests, you should move the events into the
  new file structure. Alternatively, implement the new `IFilesEventLocator` and
  provide your own custom event file layout.
* Breaking: Event stores have been split into two parts, the `IEventStore`
  and the new `IEventPersistence`. `IEventStore` has the same interface before
  but the implementation is now no longer responsible for persisting the events,
  only converting and serializing the persisted events. `IEventPersistence`
  handles the actual storing of events and thus if any custom event stores have
  been implemented, they should implement to the new `IEventPersistence`
  instead.
* New: Added `IEntity`, `IEntity<>` and an optional `Entity<>` that developers
  can use to implement DDD entities.

### New in 0.17.1134 (released 2015-09-28)

* Fixed: Using NuGet package `EventFlow.Autofac` causes an exception with the
  message `The type 'EventFlow.Configuration.Registrations.AutofacStartable'
  is not assignable to service 'Autofac.IStartable` during EventFlow setup

### New in 0.16.1120 (released 2015-09-27)

* Breaking: Removed `HasRegistrationFor<>` and `GetRegisteredServices()`
  from `IServiceRegistration` and added them to `IResolver` instead. The
  methods required that all service registrations went through EventFlow,
  which in most cases they will not
* Obsolete: Marked `IServiceRegistration.RegisterIfNotRegistered(...)`, use
  the `keepDefault = true` on the other `Register(...)` methods instead
* New: Major changes have been done to how EventFlow handles service
  registration and bootstrapping in order for developers to skip calling
  `CreateResolver()` (or `CreateContainer()` if using the `EventFlow.Autofac`
  package) completely. EventFlow will register its bootstrap services in the
  IoC container and configure itself whenever the container is created    
* New: Introduced `IBootstrap` interface that you can register. It has a
  single `BootAsync(...)` method that will be called as soon as the IoC
  container is ready (similar to that of `IStartable` of Autofac)
* Fixed: Correct order of service registration decorators. They are now
  applied in the same order they are applied, e.g., the _last_ registered
  service decorator will be the "outer" service
* Fixed: Added missing `ICommand<,>` interface to abstract `Command<,>` class in
  `EventFlow.Commands`.

### New in 0.15.1057 (released 2015-09-24)

* Fixed: Added `UseHangfireJobScheduler()` and marked `UseHandfireJobScheduler()`
  obsolete, fixing method spelling mistake

### New in 0.14.1051 (released 2015-09-23)

* Breaking: All `EventFlowOptions` extensions are now `IEventFlowOptions`
  instead and `EventFlowOptions` implements this interface. If you have made
  your own extensions, you will need to use the newly created interface
  instead. Changed in order to make testing of extensions and classes
  dependent on the EventFlow options easier to test
* New: You can now bundle your configuration of EventFlow into modules that
  implement `IModule` and register these by calling
  `EventFlowOptions.RegisterModule(...)`
* New: EventFlow now supports scheduled job execution via e.g. Hangfire. You
  can create your own scheduler or install the new `EventFlow.Hangfire` NuGet
  package. Read the jobs documentation for more details
* New: Created the OWIN `CommandPublishMiddleware` middleware that can
  handle publishing of commands by posting a JSON serialized command to
  e.g. `/commands/ping/1` in which `ping` is the command name and `1` its
  version. Remember to add authentication
* New: Created a new interface `ICommand<TAggregate,TIdentity,TSourceIdentity>`
  to allow developers to control the type of `ICommand.SourceId`. Using the
  `ICommand<TAggregate,TIdentity>` (or Command<TAggregate,TIdentity>)
  will still yield the same result as before, i.e., `ICommand.SourceId` being
  of type `ISourceId`
* New: The `AddDefaults(...)` now also adds the command type definition to the
  new `ICommandDefinitonService`

### New in 0.13.962 (released 2015-09-13)

 * Breaking: `EventFlowOptions.AddDefaults(...)` now also adds query handlers
 * New: Added an optional `Predicate<Type>` to the following option extension
   methods that scan an `Assembly`: `AddAggregateRoots(...)`,
   `AddCommandHandlers(...)`, `AddDefaults(...)`, `AddEventUpgraders(...)`,
   `AddEvents(...)`, `AddMetadataProviders(...)`, `AddQueryHandlers(...)` and
   `AddSubscribers(...)`
 * Fixed: `EventFlowOptions.AddAggregateRoots(...)` now prevents abstract
   classes from being registered when passing `IEnumerable<Type>`
 * Fixed: Events published to RabbitMQ are now in the right order for chains
   of subscribers, if `event A -> subscriber -> command -> aggregate -> event B`,
   then the order of published events to RabbitMQ was `event B` and then
   `event A`

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
