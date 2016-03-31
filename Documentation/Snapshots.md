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
