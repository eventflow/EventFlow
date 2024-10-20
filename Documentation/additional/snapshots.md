---
title: Snapshots
---

# Snapshots

When working with long-lived aggregates, performance when loading
aggregates, and thereby making changes to them, becomes a real concern.
Consider aggregates that are comprised of several thousands of events,
some of which need to go through a rigorous
[update](../basics/event-upgrade.md) process before they are applied to the
aggregates.

EventFlow supports aggregate snapshots, which is basically a capture of
the entire aggregate state every few events. So instead of loading the
entire aggregate event history, the latest snapshot is loaded, then
applied to the aggregate and then the remaining events that were not
captured in the snapshot.

To configure an aggregate root to support snapshots, inherit from
`SnapshotAggregateRoot<,,>` and define a serializable snapshot type
that is marked with the `ISnapshot` interface.

```csharp
[SnapshotVersion("user", 1)]
public class UserSnapshot : ISnapshot
{
  // Add properties and methods to represent the snapshot state
}

public class UserAggregate :
  SnapshotAggregateRoot<UserAggregate, UserId, UserSnapshot>
{
  protected override Task<UserSnapshot> CreateSnapshotAsync(
    CancellationToken cancellationToken)
  {
    // Create a UserSnapshot based on the current aggregate state
    return Task.FromResult(new UserSnapshot
    {
      // Initialize snapshot properties with the current aggregate state
    });
  }

  protected override Task LoadSnapshotAsync(
    UserSnapshot snapshot,
    ISnapshotMetadata metadata,
    CancellationToken cancellationToken)
  {
    // Load the UserSnapshot into the current aggregate
    // Update the aggregate state with the snapshot properties
    return Task.CompletedTask;
  }
}
```

When using aggregate snapshots, there are several important details to
remember:

- Aggregates must not make any assumptions regarding the existence of
  snapshots.
- Aggregates must not assume that snapshots are created with increasing
  aggregate sequence numbers.
- Snapshots must be created in such a way that they represent the
  entire history up to the point of snapshot creation.

## Snapshot strategy

When implementing an aggregate root that inherits from
`SnapshotAggregateRoot<,,>`, you need to pass the base class an
implementation of `ISnapshotStrategy`. The strategy is used to
determine when a snapshot should be created, e.g., every 100 events.

EventFlow ships with two strategies that should be enough for most purposes as they
can be configured:

- `SnapshotEveryFewVersionsStrategy:` Snapshots are created after a
  predefined number of events, the default is `100`, but another
  frequency can be specified.
- `SnapshotRandomlyStrategy:` Snapshots are created randomly with a
  predefined chance, the default is `1%`, but another can be
  specified.

## Upgrading snapshots

As an application grows over time, the data required to be stored within
a snapshot will change. Either because some become obsolete or merely
because a better way of storing the aggregate state is found. If this
happens, the snapshots persisted in the snapshot store could potentially
become useless as aggregates are unable to apply them. The easy solution
would be to make change-by-addition and make sure that the old snapshots
can be deserialized into the new version.

EventFlow provides an alternative solution, which is basically allowing
developers to upgrade snapshots similar to how [events are
upgraded](../basics/event-upgrade.md).

Let's say we have an application that has developed three snapshot
versions over time.

```csharp
[SnapshotVersion("user", 1)]
public class UserSnapshotV1 : ISnapshot
{
  // Add properties and methods to represent the snapshot state for version 1
}

[SnapshotVersion("user", 2)]
public class UserSnapshotV2 : ISnapshot
{
  // Add properties and methods to represent the snapshot state for version 2
}

[SnapshotVersion("user", 3)]
public class UserSnapshot : ISnapshot
{
  // Add properties and methods to represent the snapshot state for version 3
}
```

Note how version three of the `UserAggregate` snapshot is called
`UserSnapshot` and not `UserSnapshotV3`, it's basically to help
developers tell which snapshot version is the current one.

Remember to add the `[SnapshotVersion]` attribute as it enables
control of the snapshot definition name. If left out, EventFlow will
make a guess, which will be tied to the name of the class type.

The next step will be to implement upgraders, or mappers, that can
upgrade one snapshot to another.

```csharp
public class UserSnapshotV1ToV2Upgrader :
  ISnapshotUpgrader<UserSnapshotV1, UserSnapshotV2>
{
  public Task<UserSnapshotV2> UpgradeAsync(
    UserSnapshotV1 userSnapshotV1,
    CancellationToken cancellationToken)
  {
    // Map from V1 to V2 and return
    return Task.FromResult(new UserSnapshotV2
    {
      // Initialize properties with the mapped values from V1
    });
  }
}

public class UserSnapshotV2ToV3Upgrader :
  ISnapshotUpgrader<UserSnapshotV2, UserSnapshot>
{
  public Task<UserSnapshot> UpgradeAsync(
    UserSnapshotV2 userSnapshotV2,
    CancellationToken cancellationToken)
  {
    // Map from V2 to V3 and return
    return Task.FromResult(new UserSnapshot
    {
      // Initialize properties with the mapped values from V2
    });
  }
}
```

The snapshot types and upgraders then only need to be registered in
EventFlow.

```csharp
  // ...
  .AddSnapshotUpgraders(myAssembly)
  .AddSnapshots(myAssembly)
  // ...
```

Now, whenever a snapshot is loaded from the snapshot store, it is
automatically upgraded to the latest version and the aggregate only
needs to concern itself with the latest version.

## Snapshot store implementations

EventFlow has built-in support for some snapshot stores (more *will* be
implemented).

### Null (or none)

The default implementation used by EventFlow does absolutely nothing
besides logging a warning if used. It exists only to help developers to
select a proper snapshot store. Making in-memory the default
implementation could present problems if snapshots were configured, but
the snapshot store configuration forgotten.

### In-memory

For testing, or small applications, the in-memory snapshot store is
configured by merely calling `UseInMemorySnapshotStore()`.
```csharp
  // ...
  .UseInMemorySnapshotStore()
  // ...
```

### Microsoft SQL Server

To use the MSSQL snapshot store, you need to install the NuGet package
`EventFlow.MsSql`.

#### Configuration

Configure the MSSQL connection and snapshot store as shown here.

```csharp
public void ConfigureServices(IServiceCollection services)
{
  services.AddEventFlow(ef =>
  {
    ef.ConfigureMsSql(MsSqlConfiguration.New
      .SetConnectionString(@"Server=.\SQLEXPRESS;Database=MyApp;User Id=sa;Password=???"))
    .UseMsSqlSnapshotStore();
  });
}
```

Note that if you already use MSSQL for the event- or read model store, you
only need to invoke the `ConfigureMsSql` extension *once*.

#### Create and migrate required MSSQL databases

Before you can use the MSSQL snapshot store, the required database and
tables must be created. The database specified in your MSSQL connection
*will not* be automatically created, you have to do this yourself.

To make EventFlow create the required tables, execute the following
code.

```csharp
var msSqlDatabaseMigrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
EventFlowSnapshotStoresMsSql.MigrateDatabase(msSqlDatabaseMigrator);
```

You should do this either on application start or preferably upon
application install or update, e.g., when the website is installed.

### Custom

If none of the above stores are adequate, a custom implementation is
possible by implementing the interface `ISnapshotPersistence`.
However, there are some rules that the snapshot persistence store *must*
follow:

- It's valid to store snapshots in any order, e.g., first version 3 then 2.
- It's valid to overwrite existing snapshot versions, e.g., storing version 3 then version 3 again.
- Fallback to old snapshots is allowed.
