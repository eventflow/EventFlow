# Snapshots

When working with long-lived aggregates, performance when loading aggregates,
and thereby making changes to them, becomes a real concern.
Consider aggregates that are comprised of several
thousands of events, some of which needs to go through a rigorous
[update](./EventUpgrade.md) process before they are applied to the aggregates.

EventFlow support aggregate snapshots, which is basically a capture of the
entire aggregate state every few events. So instead of loading the entire
aggregate event history, the latest snapshot is loaded, applied to the aggregate
and then the remaining events that wasn't captured in the snapshot.

```csharp
public class UserAggregate :
  SnapshotAggregateRoot<UserAggregate, UserId, UserSnapshot>
{
  protected override Task<UserSnapshot> CreateSnapshotAsync(
    CancellationToken cancellationToken)
  {
    // Create a UserSnapshot based on the current aggregate state
    ...
  }

  protected override Task LoadSnapshotAsync(
    UserSnapshot snapshot,
    ISnapshotMetadata metadata,
    CancellationToken cancellationToken)
  {
    // Load the UserSnapshot into the current aggregate
    ...
  }
}
```

When using aggregate snapshots there are several important details to remember

* Aggregates must not make any assumptions regarding the existence of snapshots
* Aggregates must not assume that if a snapshots are created with increasing
  aggregate sequence numbers
* Snapshots must be created in such a way, that the represent the entire
  history up to the point of snapshot creation

## Upgrading snapshots

As snapshots are persisted to storage

## Snapshot store implementations

EventFlow has built-in support for some snapshot stores.

### Null (or none)

The default implementation used by EventFlow does absolutely nothing besides
logging a warning if used. It exist only to help developers to select a proper
snapshot store. Making in-memory the default implementation could present
problems if snapshots were configured, but the snapshot store configuration
forgotten.

### In-memory

For testing, or small applications, the in-memory snapshot store is configured
by merely calling `UseInMemorySnapshotStore()`.

```csharp
var resolver = EventFlowOptions.New
  ...
  .UseInMemorySnapshotStore()
  ...
  .CreateResolver();
```

### Custom

If none of the above stores are adequate, a custom implementation is possible
by implementing the interface `ISnapshotPersistence`. However, there are
some rules that the snapshot persistence must follow.

* Its valid to store snapshots in any order, e.g. first version 3 then 2
* Its valid to overwrite existing snapshots version, e.g. storing version 3
  then version 3 again
* Fallback to old snapshots is allowed
